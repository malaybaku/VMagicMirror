using MediaPipe.HandPose;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.IK
{
    /// <summary>
    /// HandPoseBarracudaのトラッキングをやるやつ。
    /// このクラスは画像処理までのラッパーで、処理結果を<see cref="BarracudaHandIK"/>に渡すことでIKが計算される
    /// </summary>
    public class BarracudaHand : MonoBehaviour
    {
        //この回数だけ連続でハンドトラッキングのスコアが閾値を超えたとき、トラッキング開始と見なす。
        //チャタリングのあるケースを厳し目に判定するために用いる
        private const int TrackingStartCount = 3;
        
        /// <summary> 手の検出の更新をサボる頻度オプション </summary>
        public enum FrameSkipStyles
        {
            //サボらず、毎フレームでLRを両方とも処理する。一番重たい
            BothOnSingleFrame,
            //サボらず、LとRを交互に処理し続ける
            LR,
            //Lを2フレームかけて処理後、Rを2フレームかけて処理、という形で交互に処理する
            LLRR,
        }

        [SerializeField] ResourceSet _resources = null;
        [SerializeField] bool _useAsyncReadback = true;
        [SerializeField] private BarracudaHandIK ik = null;
        
        [Range(0f, 1f)] [SerializeField] private float scoreThreshold = 0.5f;
        [SerializeField] private FrameSkipStyles skipStyle = FrameSkipStyles.LLRR;
        [SerializeField] private Vector2Int resolution = new Vector2Int(640, 360);
        [Range(0.5f, 1f)] [SerializeField] private float textureWidthRateForOneHand = 0.6f;

        [Header("Misc")]
        [SerializeField] private float dataSendInterval = 0.1f;
        // Debugでのみ使うやつ
        // [SerializeField] private RawImage webcamImage = null; 
        // [SerializeField] private RawImage leftImage = null; 
        // [SerializeField] private RawImage rightImage = null; 
        
        //NOTE: 画像ハンドトラッキングにおけるLeft/Rightという呼称がややこしいので要注意。左右がぐるんぐるんします
        // - webCamのleft/right -> テクスチャを左右に分割しているだけ。
        //  - rightTextureにはユーザーの左手が映っています(ユーザーが手をクロスさせたりしない限りは)。
        // - pipelineおよびHandPointsのleft/right -> ユーザーの左手/右手を解析するためのパイプラインだよ、という意味。
        //  - leftPipelineはrightTextureを受け取ることで、ユーザーの左手の姿勢を解析します。
        // - MPHandIK.HandStateのleft/right -> VRMの左手、右手のこと。
        //  - デフォルト設定では左右反転をするため、leftPipelineの結果をrightHandStateに適用します。
        
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

        private bool _imageProcessEnabled = false;
        private bool ImageProcessEnabled
        {
            get => _imageProcessEnabled;
            set
            {
                _imageProcessEnabled = value;
                ik.ImageProcessActive = HasWebCamTexture && ImageProcessEnabled;
            }
        }
        
        private bool _hasWebCamTexture = false;
        private bool HasWebCamTexture
        {
            get => _hasWebCamTexture;
            set
            {
                _hasWebCamTexture = value;
                ik.ImageProcessActive = HasWebCamTexture && ImageProcessEnabled;
            }
        }
        
        private bool _disableHorizontalFlip;
        public bool DisableHorizontalFlip 
        { 
            get => _disableHorizontalFlip;
            set
            {
                _disableHorizontalFlip = value;
                ik.DisableHorizontalFlip = value;
            } 
        }

        public bool SendResult { get; set; }

        public AlwaysDownHandIkGenerator DownHand
        {
            get => ik.DownHand;
            set => ik.DownHand = value;
        }

        public IHandIkState RightHandState => ik.RightHandState;
        public IHandIkState LeftHandState => ik.LeftHandState;

        private HandTrackingResultBuilder _resultBuilder;
        private float _resultSendCount = 0f;

        private float _leftScore = 0f;
        private float _rightScore = 0f;
        private int _leftTrackedCount = 0;
        private int _rightTrackedCount = 0;

        //NOTE: 複数フレームにわたって画像処理するシナリオについて、途中でGraphics.Blitするのを禁止するためのフラグ
        private bool _leftBlitBlock = false;
        private bool _rightBlitBlock = false;
        private float _leftTextureDt = -1f;
        private float _rightTextureDt = -1f;
        
        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable,
            IMessageReceiver receiver,
            IMessageSender sender,
            FaceTracker faceTracker
            )
        {
            _resultBuilder = new HandTrackingResultBuilder(sender);
            ik.Initialize(vrmLoadable, _leftHandPoints, _rightHandPoints);

            vrmLoadable.VrmLoaded += _ => _hasModel = true;
            vrmLoadable.VrmDisposing += () => _hasModel = false;
            
            receiver.AssignCommandHandler(
                VmmCommands.EnableImageBasedHandTracking,
                c => ImageProcessEnabled = c.ToBoolean()
            );
            
            receiver.AssignCommandHandler(
                VmmCommands.DisableHandTrackingHorizontalFlip,
                c => DisableHorizontalFlip = c.ToBoolean()
            );
            receiver.AssignCommandHandler(
                VmmCommands.EnableSendHandTrackingResult,
                c => SendResult = c.ToBoolean()
                );

            _leftTexture = new RenderTexture((int) (resolution.x * textureWidthRateForOneHand), resolution.y, 0);
            _rightTexture = new RenderTexture((int) (resolution.x * textureWidthRateForOneHand), resolution.y, 0);
            faceTracker.WebCamTextureInitialized += SetWebcamTexture;
            faceTracker.WebCamTextureDisposed += DisposeWebCamTexture;
        }

        public void SetupDependency(HandIkGeneratorDependency dependency) => ik.SetupDependency(dependency);
        
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
        }
        
        private void Update()
        {
            if (!ImageProcessEnabled || !_hasWebCamTexture || !_hasModel)
            {
                _leftBlitBlock = false;
                _rightBlitBlock = false;
                _leftTrackedCount = 0;
                _rightTrackedCount = 0;
                return;
            }

            _leftPipeline.UseAsyncReadback = _useAsyncReadback;
            _rightPipeline.UseAsyncReadback = _useAsyncReadback;

            BlitTextures();
            CallHandUpdate();
            SendHandTrackingResult();
            
            //DEBUG
            // webcamImage.texture = _webCamTexture;
            // leftImage.texture = _leftTexture;
            // rightImage.texture = _rightTexture;

            ik.UpdateIk();
        }

        private void OnDestroy()
        {
            _leftPipeline.Dispose();
            _rightPipeline.Dispose();
        }

        private void BlitTextures()
        {
            if (!_webCamTexture.didUpdateThisFrame)
            {
                return;
            }

            //TODO: アス比によって絵が崩れる問題の対策
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

        private void CallHandUpdate()
        {
            _frameCount++;
            switch (skipStyle)
            {
                case FrameSkipStyles.BothOnSingleFrame:
                    _frameCount = 0;
                    UpdateLeftHandBefore();
                    UpdateLeftHandAfter();
                    UpdateRightHandBefore();
                    UpdateRightHandAfter();
                    break;
                case FrameSkipStyles.LR:
                    if (_frameCount > 1)
                    {
                        UpdateRightHandBefore();
                        UpdateRightHandAfter();
                        _frameCount = 0;
                    }
                    else
                    {
                        UpdateLeftHandBefore();
                        UpdateLeftHandAfter();
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
            _leftScore = pipeline.Score;
            if (_leftScore < scoreThreshold)
            {
                _leftTrackedCount = 0;
                return;
            }

            _leftTrackedCount++;
            if (_leftTrackedCount < TrackingStartCount)
            {
                return;
            }
            
            for (var i = 0; i < HandPipeline.KeyPointCount; i++)
            {
                //XとZをひっくり返すと鏡像的なアレが直る
                var p = pipeline.GetKeyPoint(i);
                _leftHandPoints[i] = new Vector3(-p.x, p.y, -p.z);
            }

            ik.UpdateLeftHand();
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
            _rightScore = pipeline.Score;
            if (_rightScore < scoreThreshold)
            {
                _rightTrackedCount = 0;
                return;
            }

            _rightTrackedCount++;
            if (_rightTrackedCount < TrackingStartCount)
            {
                return;
            }
            
            for (var i = 0; i < HandPipeline.KeyPointCount; i++)
            {
                var p = pipeline.GetKeyPoint(i);
                _rightHandPoints[i] = new Vector3(-p.x, p.y, -p.z);
            }

            ik.UpdateRightHand();
        }
        
        private void SendHandTrackingResult()
        {
            if (!SendResult)
            {
                _resultSendCount = 0f;
                return;
            }

            _resultSendCount += Time.deltaTime;
            if (_resultSendCount < dataSendInterval)
            {
                return;
            }
            _resultSendCount -= dataSendInterval;
            
            _resultBuilder.SendResult(
                _leftScore >= scoreThreshold,
                _leftScore,
                _leftHandPoints,
                _rightScore >= scoreThreshold,
                _rightScore,
                _rightHandPoints
                );
        }
    }
}
