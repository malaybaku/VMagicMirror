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
    /// 他の処理に比べるとMediaPipeへの依存度が妙に高いのでフォルダを分けてます
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
            //L、R、スキップ、L、R、スキップ、…
            LR_Skip,
            //L、スキップ、R、スキップ、…
            L_Skip_R_Skip,
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

        [Range(0f, 1f)]
        [SerializeField] private float scoreThreshold = 0.5f;

        //NOTE: L/Rの検出を1フレームずつやった後に何もしないフレームを入れる、その何もしないフレーム数
        [SerializeField] private FrameSkipStyles skipStyle = FrameSkipStyles.L_Skip_R_Skip;

        [SerializeField] private float positionSmoothFactor = 12f;
        [SerializeField] private float rotationSmoothFactor = 12f;
        [SerializeField] private Vector2Int resolution = new Vector2Int(640, 360);
        [Range(0.5f, 1f)]
        [SerializeField] private float textureWidthRateForOneHand = 0.6f;

        private Vector3 leftHandOffset => new Vector3(-rightHandOffset.x, rightHandOffset.y, rightHandOffset.z);

        #region 入力される参考情報 - 生の解析結果くらいまでのデータ
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

        private MPHandState _rightHandState = new MPHandState(ReactedHand.Right);
        public IHandIkState RightHandState => _rightHandState;
        private MPHandState _leftHandState = new MPHandState(ReactedHand.Left);
        public IHandIkState LeftHandState => _leftHandState;
        
        private Vector3 _leftPosTarget;
        private Quaternion _leftRotTarget;
        private Vector3 _rightPosTarget;
        private Quaternion _rightRotTarget;

        private MPHandFinger _finger;
        
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

            _leftTexture = new RenderTexture((int)(resolution.x * textureWidthRateForOneHand), resolution.y, 0);
            _rightTexture = new RenderTexture((int)(resolution.x * textureWidthRateForOneHand), resolution.y, 0); 
            faceTracker.WebCamTextureInitialized += SetWebcamTexture;
            faceTracker.WebCamTextureDisposed += DisposeWebCamTexture;
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
                return;
            }
            
            _leftPipeline.UseAsyncReadback = _useAsyncReadback;
            _rightPipeline.UseAsyncReadback = _useAsyncReadback;
            
            //TODO: デバッグ終わったらオフ
            previewImage.texture = _webCamTexture;
            
            BlitTextures();
            
            //NOTE: webcamのdidUpdateThisFrameを見てガードしてもいい…が、LSRS型のアプデなら不要かなあ。
            CallHandUpdate();

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
                var aspect1 = (float)_webCamTexture.width / _webCamTexture.height;
                var aspect2 = (float)resolution.x / resolution.y;
                var gap = aspect2 / aspect1;
                var vflip = _webCamTexture.videoVerticallyMirrored;
                var scale = new Vector2(gap, vflip ? -1 : 1);
                var offset = new Vector2((1 - gap) / 2, vflip ? 1 : 0);

                //L/R Image Blit
                var scaleToBlit = new Vector2(scale.x * textureWidthRateForOneHand, scale.y);
                Graphics.Blit(_webCamTexture, _leftTexture, scaleToBlit, offset);
                Graphics.Blit(
                    _webCamTexture, _rightTexture, scaleToBlit, 
                    new Vector2(offset.x + (1f - textureWidthRateForOneHand), offset.y)
                    );
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
                    case FrameSkipStyles.LR_Skip:
                        switch (_frameCount)
                        {
                            case 1:
                                UpdateLeftHand();
                                break;
                            case 2:
                                UpdateRightHand();
                                break;
                            default:
                                _frameCount = 0;
                                break;
                        }
                        break;
                    case FrameSkipStyles.L_Skip_R_Skip:
                        switch (_frameCount)
                        {
                            case 1:
                                UpdateLeftHand();
                                break;
                            case 2:
                                break;
                            case 3:
                                UpdateRightHand();
                                break;
                            case 4:
                            default:
                                _frameCount = 0;
                                break;
                        }
                        break;
                }
            }
        }

        private void UpdateLeftHand()
        {
            var pipeline = _leftPipeline;
            
            //NOTE: 左手はwebcam画像の右側に映っている。
            pipeline.ProcessImage(_rightTexture);
            
            if (pipeline.Score < scoreThreshold)
            {
                return;
            }
            
            for (var i = 0; i < HandPipeline.KeyPointCount; i++)
            {
                //XとZをひっくり返すと鏡像的なアレが直るはず。Xは鏡写しがデフォならどうすべきか、というのがあるが…
                var p = pipeline.GetKeyPoint(i);
                _leftHandPoints[i] = new Vector3(-p.x, p.y, -p.z);
            }

            _leftPosTarget = 
                _defaultHeadPosition + commonAdditionalOffset + 
                Mul(motionScale, leftHandOffset + _leftHandPoints[0]);
            
            var rotInfo = CalculateLeftHandRotation();
            _leftRotTarget = rotInfo.Item1;
            
            _finger.UpdateLeft(rotInfo.Item2, rotInfo.Item3);
            _leftHandState.RaiseRequestToUse();
        }

        private void UpdateRightHand()
        {
            var pipeline = _rightPipeline;
            
            pipeline.ProcessImage(_leftTexture);
            
            if (pipeline.Score < scoreThreshold)
            {
                return;
            }
            
            for (var i = 0; i < HandPipeline.KeyPointCount; i++)
            {
                var p = pipeline.GetKeyPoint(i);
                _rightHandPoints[i] = new Vector3(-p.x, p.y, -p.z);
            }
            
            _rightPosTarget =
                _defaultHeadPosition + commonAdditionalOffset + 
                Mul(motionScale, rightHandOffset + _rightHandPoints[0]);

            var rotInfo = CalculateRightHandRotation();
            _rightRotTarget = rotInfo.Item1;
            
            _finger.UpdateRight(rotInfo.Item2, rotInfo.Item3);
            _rightHandState.RaiseRequestToUse();
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
                Quaternion.LookRotation(wristForward, wristUp) *
                Quaternion.AngleAxis(90f, Vector3.up);

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
                Quaternion.LookRotation(wristForward, wristUp) *
                Quaternion.AngleAxis(-90f, Vector3.up);

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
                    Finger?.ReleaseLeftHand();
                }
                else
                {
                    Finger?.ReleaseRightHand();
                }
            }
        }
    }
}
