using System;
using MediaPipe.HandPose;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Baku.VMagicMirror.IK
{
    /// <summary>
    /// HandPoseBarracudaのトラッキングをVRMの手で以下に割り当てるすごいやつだよ
    /// - 手首IK
    /// - 指曲げFK
    /// </summary>
    /// <remarks>
    /// - 他の処理に比べるとMediaPipeへの依存度が妙に高いのでフォルダを分けてます
    /// </remarks>
    public class MPHand : MonoBehaviour
    {
        /// <summary>
        /// 手の検出の更新をサボる頻度
        /// </summary>
        public enum FrameSkipStyles
        {
            //サボらず、毎フレームでLRを両方とも処理する。一番重たい
            BothOnSingleFrame,

            //サボらず、LとRを交互に処理し続ける
            LR,

            //LとRを、2フレームずつかけて交互に処理する、スキップ、R、スキップ、…
            LLRR,
        }

        [SerializeField] ResourceSet _resources = null;
        [SerializeField] bool _useAsyncReadback = true;
        [SerializeField] private FingerController _fingerController = null;
        [SerializeField] private RawImage previewImage = null;
        [SerializeField] private RawImage previewLImage = null;
        [SerializeField] private RawImage previewRImage = null;

        //NOTE: 画像をタテに切っている関係で、画面中央に映った手 = だいたい肩の正面くらいに手があるのでは？とみなしたい
        [SerializeField] private Vector3 rightHandOffset = new Vector3(0.25f, 0f, 0f);

        //NOTE: なぜズラすかというと、wrist自体にはz座標が入っていないため。
        //そこで、UpperArm - LowerArmの距離に相当する程度の距離だけ+zに手をズラす
        [SerializeField] private Vector3 commonAdditionalOffset = new Vector3(0f, 0f, 0.25f);

        //手と頭の距離にスケールをかけると、実際には頭の脇でちょこちょこ動かすだけのムーブを大きくできる
        [SerializeField] private Vector3 motionScale = Vector3.one;

        [Range(0f, 1f)] [SerializeField] private float scoreThreshold = 0.5f;

        //NOTE: L/Rの検出を1フレームずつやった後に何もしないフレームを入れる、その何もしないフレーム数
        [SerializeField] private FrameSkipStyles skipStyle = FrameSkipStyles.LLRR;

        [SerializeField] private float positionSmoothFactor = 12f;
        [SerializeField] private float rotationSmoothFactor = 12f;
        [SerializeField] private Vector2Int resolution = new Vector2Int(640, 360);
        [Range(0.5f, 1f)] [SerializeField] private float textureWidthRateForOneHand = 0.6f;

        //この秒数だけトラッキングが更新されなかったら手を下ろす
        [SerializeField] private float lostCount = 1f;
        //トラッキングロスト時の手下ろし速度のファクター
        [SerializeField] private float lostLerpFactor = 3f;
        

        private Vector3 leftHandOffset => new Vector3(-rightHandOffset.x, rightHandOffset.y, rightHandOffset.z);

        #region 入力される参考情報 - 生の解析結果くらいまでのデータ

        //NOTE: Left/Rightという呼称がかなりややこしいので要注意。左右がぐるんぐるんします
        // - webCamのleft/right -> テクスチャを左右に分割しているだけ。
        //  - rightTextureにはユーザーの左手が映っています(ユーザーが手をクロスさせたりしない限りは)。
        // - pipelineおよびHandPointsのleft/right -> ユーザーの左手/右手を解析するためのパイプラインだよ、という意味。
        //  - leftPipelineはrightTextureを受け取ることで、ユーザーの左手の姿勢を解析します。
        // - HandStateのleft/right -> VRMの左手、右手のこと。
        //  - デフォルト設定では左右反転をするため、leftPipelineの結果をrightHandStateに適用します。
        
        private bool _hasWebCamTexture = false;
        private WebCamTexture _webCamTexture;
        private RenderTexture _leftTexture;
        private RenderTexture _rightTexture;

        private HandPipeline _leftPipeline = null;
        private HandPipeline _rightPipeline = null;

        //NOTE: スムージングしたくなりそうなので分けておく
        private readonly Vector3[] _leftHandPoints = new Vector3[HandPipeline.KeyPointCount];
        private readonly Vector3[] _rightHandPoints = new Vector3[HandPipeline.KeyPointCount];
        public Vector3[] LeftHandPoints => _leftHandPoints;

        private int _frameCount = 0;

        private bool _hasModel = false;
        private Transform _head = null;
        private Vector3 _defaultHeadPosition = Vector3.up;

        #endregion

        public bool ImageProcessEnabled { get; private set; } = false;
        
        public bool DisableHorizontalFlip { get; set; }

        public AlwaysDownHandIkGenerator DownHand { get; set; }
        
        private MPHandState _rightHandState = new MPHandState(ReactedHand.Right);
        public IHandIkState RightHandState => _rightHandState;
        private MPHandState _leftHandState = new MPHandState(ReactedHand.Left);
        public IHandIkState LeftHandState => _leftHandState;

        private Vector3 _leftPosTarget;
        private Quaternion _leftRotTarget;
        private Vector3 _rightPosTarget;
        private Quaternion _rightRotTarget;

        private MPHandFinger _finger;
        private HandIkGeneratorDependency _dependency;

        private float _leftLostCount = 0f;
        private float _rightLostCount = 0f;

        //NOTE: 複数フレームにわたって画像処理するシナリオについて、途中でGraphics.Blitするのを禁止するためのフラグ
        private bool _leftBlitBlock = false;
        private bool _rightBlitBlock = false;
        private float _leftTextureDt = -1f;
        private float _rightTextureDt = -1f;

        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, IMessageReceiver receiver, FaceTracker faceTracker)
        {
            vrmLoadable.VrmLoaded += info =>
            {
                _head = info.animator.GetBoneTransform(HumanBodyBones.Head);
                _defaultHeadPosition = _head.position;
                _hasModel = true;
            };

            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _head = null;
            };

            receiver.AssignCommandHandler(
                VmmCommands.EnableImageBasedHandTracking,
                c => ImageProcessEnabled = c.ToBoolean()
            );
            
            receiver.AssignCommandHandler(
                VmmCommands.DisableHandTrackingHorizontalFlip,
                c => DisableHorizontalFlip = c.ToBoolean()
            );

            _leftTexture = new RenderTexture((int) (resolution.x * textureWidthRateForOneHand), resolution.y, 0);
            _rightTexture = new RenderTexture((int) (resolution.x * textureWidthRateForOneHand), resolution.y, 0);
            faceTracker.WebCamTextureInitialized += SetWebcamTexture;
            faceTracker.WebCamTextureDisposed += DisposeWebCamTexture;
        }

        public void SetupDependency(HandIkGeneratorDependency dependency)
        {
            _dependency = dependency;
        }
        
        private void SetWebcamTexture(WebCamTexture webCam)
        {
            DisposeWebCamTexture();
            _webCamTexture = webCam;
            _hasWebCamTexture = true;
        }

        private void DisposeWebCamTexture()
        {
            _hasWebCamTexture = false;
            _webCamTexture = null;
        }

        private void Start()
        {
            //NOTE: パイプラインは2つ要る。使いまわしできないので注意
            _leftPipeline = new HandPipeline(_resources);
            _rightPipeline = new HandPipeline(_resources);

            _leftPosTarget = _leftHandState.IKData.Position;
            _leftRotTarget = _leftHandState.IKData.Rotation;
            _rightPosTarget = _rightHandState.IKData.Position;
            _rightRotTarget = _rightHandState.IKData.Rotation;
            _finger = new MPHandFinger(_fingerController, _leftHandPoints, _rightHandPoints);
            _leftHandState.Finger = _finger;
            _rightHandState.Finger = _finger;

            //TODO: デバッグ終わったらオフ
            previewLImage.texture = _leftTexture;
            previewRImage.texture = _rightTexture;
        }

        private void OnDestroy()
        {
            _leftPipeline.Dispose();
            _rightPipeline.Dispose();
        }

        private void Update()
        {
            if (!ImageProcessEnabled || !_hasWebCamTexture || !_hasModel)
            {
                _leftBlitBlock = false;
                _rightBlitBlock = false;
                return;
            }

            _leftPipeline.UseAsyncReadback = _useAsyncReadback;
            _rightPipeline.UseAsyncReadback = _useAsyncReadback;

            //TODO: デバッグ終わったらオフ
            previewImage.texture = _webCamTexture;

            BlitTextures();
            CallHandUpdate();

            _leftLostCount += Time.deltaTime;
            _rightLostCount += Time.deltaTime;
            var lostFactor = lostLerpFactor * Time.deltaTime;

            if ((_leftLostCount > lostCount && DisableHorizontalFlip) ||
                (_rightLostCount > lostCount && !DisableHorizontalFlip))
            {
                _leftPosTarget = Vector3.Lerp(_leftPosTarget, DownHand.LeftHand.Position, lostFactor);
                _leftRotTarget = Quaternion.Slerp(_leftRotTarget, DownHand.LeftHand.Rotation, lostFactor);
            }
            
            if ((_rightLostCount > lostCount && DisableHorizontalFlip) ||
                (_leftLostCount > lostCount && !DisableHorizontalFlip))
            {
                _rightPosTarget = Vector3.Lerp(_rightPosTarget, DownHand.RightHand.Position, lostFactor);
                _rightRotTarget = Quaternion.Slerp(_rightRotTarget, DownHand.RightHand.Rotation, lostFactor);
            }

            _leftHandState.IKData.Position = Vector3.Lerp(
                _leftHandState.IKData.Position, _leftPosTarget, positionSmoothFactor * Time.deltaTime
            );
            _leftHandState.IKData.Rotation = Quaternion.Slerp(
                _leftHandState.IKData.Rotation, _leftRotTarget, rotationSmoothFactor * Time.deltaTime
            );

            _rightHandState.IKData.Position = Vector3.Lerp(
                _rightHandState.IKData.Position, _rightPosTarget, positionSmoothFactor * Time.deltaTime
            );
            _rightHandState.IKData.Rotation = Quaternion.Slerp(
                _rightHandState.IKData.Rotation, _rightRotTarget, rotationSmoothFactor * Time.deltaTime
            );
            
            void BlitTextures()
            {
                if (!_webCamTexture.didUpdateThisFrame)
                {
                    return;
                }

                //TODO: アス比によって絵が崩れる問題の対策。超重要
                var aspect1 = (float) _webCamTexture.width / _webCamTexture.height;
                var aspect2 = (float) resolution.x / resolution.y;
                var gap = aspect2 / aspect1;
                var vflip = _webCamTexture.videoVerticallyMirrored;
                var scale = new Vector2(gap, vflip ? -1 : 1);
                var offset = new Vector2((1 - gap) / 2, vflip ? 1 : 0);

                //L/R Image Blit
                var scaleToBlit = new Vector2(scale.x * textureWidthRateForOneHand, scale.y);
                if (!_leftBlitBlock)
                {
                    Graphics.Blit(_webCamTexture, _leftTexture, scaleToBlit, offset);
                }

                if (!_rightBlitBlock)
                {
                    Graphics.Blit(
                        _webCamTexture, _rightTexture, scaleToBlit,
                        new Vector2(offset.x + (1f - textureWidthRateForOneHand), offset.y)
                    );
                }
            }

            void CallHandUpdate()
            {
                _frameCount++;
                switch (skipStyle)
                {
                    case FrameSkipStyles.BothOnSingleFrame:
                        _frameCount = 0;
                        UpdateLeftHand();
                        UpdateRightHand();
                        break;
                    case FrameSkipStyles.LR:
                        if (_frameCount > 1)
                        {
                            UpdateRightHand();
                            _frameCount = 0;
                        }
                        else
                        {
                            UpdateLeftHand();
                        }

                        break;
                    case FrameSkipStyles.LLRR:
                        switch (_frameCount)
                        {
                            case 1:
                                UpdateLeftHandBefore();
                                break;
                            case 2:
                                UpdateLeftHandAfter();
                                break;
                            case 3:
                                UpdateRightHandBefore();
                                break;
                            case 4:
                            default:
                                UpdateRightHandAfter();
                                _frameCount = 0;
                                break;
                        }

                        break;
                }
            }
        }

        private void UpdateLeftHandBefore()
        {
            //NOTE: 左手はwebcam画像の右側に映っているので、右テクスチャを固定しつつ見に行く
            _rightBlitBlock = true;
            _rightTextureDt = Time.deltaTime;
            _leftPipeline.DetectPalm(_rightTexture, _rightTextureDt);
        }

        private void UpdateLeftHandAfter()
        {
            //NOTE: 左手はwebcam画像の右側に映っているので、前半と同じく右テクスチャを見に行く
            _rightBlitBlock = false;
            _leftPipeline.CalculateLandmarks(_rightTexture, _rightTextureDt);

            //解析が終わった = いろいろ見ていく。ただしスコアが低いのは無視
            var pipeline = _leftPipeline;
            if (pipeline.Score < scoreThreshold)
            {
                return;
            }
            _leftLostCount = 0f;
            
            for (var i = 0; i < HandPipeline.KeyPointCount; i++)
            {
                //XとZをひっくり返すと鏡像的なアレが直るはず。Xは鏡写しがデフォならどうすべきか、というのがあるが…
                var p = pipeline.GetKeyPoint(i);
                _leftHandPoints[i] = new Vector3(-p.x, p.y, -p.z);
            }

            
            var rotInfo = CalculateLeftHandRotation();
            
            if (DisableHorizontalFlip)
            {
                _leftPosTarget = 
                    _defaultHeadPosition + commonAdditionalOffset + 
                    Mul(motionScale, leftHandOffset + _leftHandPoints[0]);
                _leftRotTarget = rotInfo.Item1 * Quaternion.AngleAxis(90f, Vector3.up);
            }
            else
            {
                var p = _leftHandPoints[0];
                p.x = -p.x;
                _rightPosTarget =
                    _defaultHeadPosition + commonAdditionalOffset + 
                    Mul(motionScale, rightHandOffset + p);
                var rightRot = rotInfo.Item1;
                rightRot.y *= -1f;
                rightRot.z *= -1f;
                _rightRotTarget = rightRot * Quaternion.AngleAxis(-90f, Vector3.up);
            }
            
            if (DisableHorizontalFlip)
            {
                _leftHandState.RaiseRequestToUse();
            }
            else
            {
                _rightHandState.RaiseRequestToUse();
            }

            //NOTE: 状態をチェックすることにより、「つねに手下げモード」時とかに指が動いてしまうのを防ぐ
            if ((DisableHorizontalFlip && _dependency.Config.LeftTarget.Value == HandTargetType.ImageBaseHand) ||
                (!DisableHorizontalFlip && _dependency.Config.RightTarget.Value == HandTargetType.ImageBaseHand)
                )
            {
                _finger.UpdateLeft(rotInfo.Item2, rotInfo.Item3);
                _finger.ApplyLeftFingersDataToModel(DisableHorizontalFlip);
            }
        }

        private void UpdateLeftHand()
        {
            UpdateLeftHandBefore();
            UpdateLeftHandAfter();
        }

        private void UpdateRightHandBefore()
        {
            _leftBlitBlock = true;
            _leftTextureDt = Time.deltaTime;
            _rightPipeline.DetectPalm(_leftTexture, _leftTextureDt);
        }

        private void UpdateRightHandAfter()
        {
            _leftBlitBlock = false;
            _rightPipeline.CalculateLandmarks(_leftTexture, _leftTextureDt);
           
            var pipeline = _rightPipeline;
            if (pipeline.Score < scoreThreshold)
            {
                return;
            }
            _rightLostCount = 0f;
            
            for (var i = 0; i < HandPipeline.KeyPointCount; i++)
            {
                var p = pipeline.GetKeyPoint(i);
                _rightHandPoints[i] = new Vector3(-p.x, p.y, -p.z);
            }

            var rotInfo = CalculateRightHandRotation();

            if (DisableHorizontalFlip)
            {
                _rightPosTarget =
                    _defaultHeadPosition + commonAdditionalOffset + 
                    Mul(motionScale, rightHandOffset + _rightHandPoints[0]);
                _rightRotTarget = rotInfo.Item1 * Quaternion.AngleAxis(-90f, Vector3.up);
            }
            else
            {
                var p = _rightHandPoints[0];
                p.x = -p.x;
                _leftPosTarget = 
                    _defaultHeadPosition + commonAdditionalOffset + 
                    Mul(motionScale, leftHandOffset + p);
                var leftRot = rotInfo.Item1;
                leftRot.y *= -1f;
                leftRot.z *= -1f;
                _leftRotTarget = leftRot * Quaternion.AngleAxis(90f, Vector3.up);
            }

            if (DisableHorizontalFlip)
            {
                _rightHandState.RaiseRequestToUse();
            }
            else
            {
                _leftHandState.RaiseRequestToUse();
            }
            
            //NOTE: 状態をチェックすることにより、「つねに手下げモード」時とかに指が動いてしまうのを防ぐ
            if ((DisableHorizontalFlip && _dependency.Config.RightTarget.Value == HandTargetType.ImageBaseHand) ||
                (!DisableHorizontalFlip && _dependency.Config.LeftTarget.Value == HandTargetType.ImageBaseHand)
                )
            {
                _finger.UpdateRight(rotInfo.Item2, rotInfo.Item3);
                _finger.ApplyRightFingersDataToModel(DisableHorizontalFlip);
            }
        }

        private void UpdateRightHand()
        {
            UpdateRightHandBefore();
            UpdateRightHandAfter();
        }
        
        //左手の取るべきワールド回転に関連して、回転、手の正面方向ベクトル、手のひらと垂直なベクトルの3つを計算します。
        private (Quaternion, Vector3, Vector3) CalculateLeftHandRotation()
        {
            var wristForward = (_leftHandPoints[9] - _leftHandPoints[0]).normalized;
            //NOTE: 右手と逆の順にすることに注意
            var wristUp = Vector3.Cross(
                _leftHandPoints[17] - _leftHandPoints[0],
                wristForward
            ).normalized;

            //NOTE: 第2項が右手と違うので注意
            var rot =
                //Quaternion.LookRotation(wristForward) *
                Quaternion.LookRotation(wristForward, wristUp);// *
                // Quaternion.AngleAxis(90f, Vector3.up);

            return (rot, wristForward, wristUp);
        }
        
        //右手の取るべきワールド回転に関連して、回転、手の正面方向ベクトル、手のひらと垂直なベクトルの3つを計算します。
        private (Quaternion, Vector3, Vector3) CalculateRightHandRotation()
        {
            //正面 = 中指方向
            var wristForward = (_rightHandPoints[9] - _rightHandPoints[0]).normalized;
            //手首と垂直 = 人差し指あるいは中指方向、および小指で外積を取ると手の甲方向のベクトルが得られる
            var wristUp = Vector3.Cross(
                wristForward, 
                _rightHandPoints[17] - _rightHandPoints[0]
            ).normalized;

            //局所座標の余ってるベクトル = 右手の親指付け根から小指付け根方向のベクトル
            // var right = Vector3.Cross(up, forward)

            //上記のwristForwardとかwristUpの考え方の前提に「中指が正面向き、手のひらが真下」という基本姿勢があるので、
            //手をそこまで持っていってから適用
            var rot = //Quaternion.LookRotation(wristForward) *
                Quaternion.LookRotation(wristForward, wristUp);// *
                //Quaternion.AngleAxis(-90f, Vector3.up);

            return (rot, wristForward, wristUp);
        }
        

        private static Vector3 Mul(Vector3 u, Vector3 v)
            => new Vector3(u.x * v.x, u.y * v.y, u.z * v.z);
        
        private static void LogVec(string vName, Vector3 v)
            => Debug.Log($"{vName} = {v.x:0.000}, {v.y:0.000}, {v.z:0.000}");

        private class MPHandState : IHandIkState
        {
            public MPHandState(ReactedHand hand)
            {
                Hand = hand;
            }
            
            public MPHandFinger Finger { get; set; }
            public bool DisableHorizontalFlip { get; set; }
            
            public IKDataRecord IKData { get; } = new IKDataRecord();

            public Vector3 Position => IKData.Position;
            public Quaternion Rotation => IKData.Rotation;
            public ReactedHand Hand { get; }
            public HandTargetType TargetType => HandTargetType.ImageBaseHand;

            public void RaiseRequestToUse() => RequestToUse?.Invoke(this);
            public event Action<IHandIkState> RequestToUse;

            public void Enter(IHandIkState prevState)
            {
                //TODO: 位置の補間をしないとダメかも
            }

            public void Quit(IHandIkState nextState)
            {
                if (Hand == ReactedHand.Left)
                {
                    if (DisableHorizontalFlip)
                    {
                        Finger?.ReleaseLeftHand();
                    }
                    else
                    {
                        Finger?.ReleaseRightHand();
                    }
                }
                else
                {
                    if (DisableHorizontalFlip)
                    {
                        Finger?.ReleaseRightHand();
                    }
                    else
                    {
                        Finger?.ReleaseLeftHand();
                    }
                }
            }
        }
    }
}
