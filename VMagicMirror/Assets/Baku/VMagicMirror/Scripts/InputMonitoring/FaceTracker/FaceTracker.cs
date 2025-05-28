using System;
using System.Collections;
using Baku.VMagicMirror.VMCP;
using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// カメラが起動しているときに回ってる顔トラッキングの種類です。
    /// </summary>
    public enum FaceTrackingMode
    {
        /// <summary> 顔トラッキングをしません。ハンドトラッキングだけ回ってる時に使います。 </summary>
        None,
        /// <summary> DlibFaceLandmarkDetectorによる低負荷な顔トラッキングを回します。 </summary>
        LowPower,
    }
    
    /// <summary>
    /// ウェブカメラを使ってVRMの表情向けのトラッキングを回す
    /// </summary>
    public class FaceTracker : MonoBehaviour
    {

        [SerializeField] private string requestedDeviceName = "";
        //NOTE: このクラス自身はテクスチャのリサイズに関知しない。
        [SerializeField] private Vector2Int rawTextureSize = new Vector2Int(640, 480);
        [SerializeField] private int requestedFps = 30;

        [Tooltip("顔トラッキングがオンであるにも関わらず表情データが降ってこない状態が続き、異常値と判定するまでの秒数")]
        [SerializeField] private float activeButNotTrackedCount = 1.0f;
        [Tooltip("顔トラッキングが外れたとき、色々なパラメータを基準位置に戻すためのLerpファクター(これにTime.deltaTimeをかけた値を用いる")]
        [SerializeField] private float notTrackedResetSpeedFactor = 3.0f;
        [Tooltip("検出処理が走る最短間隔をミリ秒単位で規定します。")]
        [SerializeField] private int trackMinIntervalMillisec = 60;
        
        /// <summary> キャリブレーションの内容 </summary>
        public CalibrationData CalibrationData { get; } = new();

        /// <summary> カメラが初期化済みかどうか </summary>
        public bool HasInitDone { get; private set; }
        private bool _isInitWaiting;

        /// <summary> カメラを起動してから1度以上顔が検出されたかどうか </summary>
        public bool FaceDetectedAtLeastOnce { get; private set; }

        //実際に接続できてるかどうかはさておき「カメラを使ってるつもり」というあいだはtrueになるフラグ。
        private FaceTrackingMode _trackingMode;
        private readonly NoneFaceAnalyzer _noneFaceAnalyzer = new();
        private DlibFaceAnalyzeRoutine _dlibFaceAnalyzer;

        public FaceAnalyzeRoutineBase CurrentAnalyzer
        {
            get
            {
                switch (_trackingMode)
                {
                    case FaceTrackingMode.LowPower: return _dlibFaceAnalyzer;
                    default: return _noneFaceAnalyzer;
                }
            }
        }

        private bool _disableHorizontalFlip;
        public bool DisableHorizontalFlip
        {
            get => _disableHorizontalFlip;
            set
            {
                _disableHorizontalFlip = value;
                _dlibFaceAnalyzer.DisableHorizontalFlip = value;
            } 
        }

        private int TrackMinIntervalMs => trackMinIntervalMillisec;

        private bool _calibrationRequested;
        private float _faceNotDetectedCountDown;

        private HorizontalFlipController _horizontalFlipController;
        private WebCamTexture _webCamTexture;
        private WebCamDevice _webCamDevice;
        private Color32[] _colors;
        
        //UIスレッドがタイミングを見計らうために使う
        private float _countFromPreviousSetColors = 0f;
        private bool _hasFrameUpdateSincePreviousSetColors = false;

        private IMessageSender _sender;
        [Inject]
        public void Initialize(
            IMessageReceiver receiver, 
            IMessageSender sender,
            HorizontalFlipController horizontalFlipController,
            VMCPActiveness vmcpActiveness
            )
        {
            var _ = new FaceTrackerReceiver(receiver, this, vmcpActiveness);
            _horizontalFlipController = horizontalFlipController;
            _sender = sender;
        }

        private void Start()
        {
            CalibrationData.SetDefaultValues();
            
            _dlibFaceAnalyzer = new DlibFaceAnalyzeRoutine(StreamingAssetFileNames.DlibFaceTrackingDataFileName);
            _dlibFaceAnalyzer.SetUp();
            
            _horizontalFlipController.DisableFaceHorizontalFlip
                .Subscribe(disable => DisableHorizontalFlip = disable)
                .AddTo(this);
        }

        private void Update()
        {
            //Update処理はランドマークの検出スレッドとの通信がメインタスク
            // 1. データを送る準備が出来たら送る
            // 2. 結果が戻ってきてたら使う。キャリブが必要ならついでに実施
            // 3. あまりに長時間何も来ない場合、トラッキングロスト扱い
            SetImageIfPrepared();
            GetDetectionResult();
            CheckTrackingLost();
            
            void SetImageIfPrepared()
            {
                _countFromPreviousSetColors += Time.deltaTime;
                if (HasInitDone && _webCamTexture.didUpdateThisFrame)
                {
                    _hasFrameUpdateSincePreviousSetColors = true;
                }

                //かなり条件が厳しい。
                //カメラ初期化して実際に画像の更新があり、前回から十分な時間経過があり、
                //しかも画像処理側のスレッドがヒマである、というのが条件
                bool canSetImage = HasInitDone &&
                   _webCamTexture.isPlaying &&
                   _hasFrameUpdateSincePreviousSetColors &&
                   _countFromPreviousSetColors > TrackMinIntervalMs * .001f &&
                   _colors != null &&
                   CurrentAnalyzer.CanRequestNextProcess;

                //どれか一つの条件が揃ってないのでダメ
                if (!canSetImage)
                {
                    return;
                }

                _countFromPreviousSetColors = 0f;
                _hasFrameUpdateSincePreviousSetColors = false;
                    
                try
                {
                    _webCamTexture.GetPixels32(_colors);
                    CurrentAnalyzer.RequestNextProcess(_colors, _webCamTexture.width, _webCamTexture.height);
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }
            
            //別スレッドの画像処理が終わっていたらその結果を受け取る
            void GetDetectionResult()
            {
                if (!CurrentAnalyzer.HasResultToApply || !HasInitDone)
                {
                    return;
                }

                //NOTE: キャリブレーションをリクエストした場合、
                //このApplyによってCalibrationDataが書き換わるはずなので、それを通知する
                CurrentAnalyzer.ApplyResult(CalibrationData, _calibrationRequested);
                if (_calibrationRequested)
                {
                    _calibrationRequested = false;
                    _sender.SendCommand(
                        MessageFactory.SetCalibrationFaceData(JsonUtility.ToJson(CalibrationData))
                        );
                }

                FaceDetectedAtLeastOnce = true;
                //顔が検出できてるので、未検出カウントダウンは最初からやり直し
                _faceNotDetectedCountDown = activeButNotTrackedCount;
            }

            //顔トラ起動中はつねに「実は顔トラッキング出来てないのでは？」というのをチェックする
            void CheckTrackingLost()
            {
                if (!HasInitDone)
                {
                    return;
                }

                
                if (_faceNotDetectedCountDown >= 0)
                {
                    _faceNotDetectedCountDown -= Time.deltaTime;
                }
                
                if (_faceNotDetectedCountDown < 0)
                {
                    //顔トラが全然更新されない -> ヘンなとこで固まってる可能性が高いので、正面姿勢に戻す
                    float lerpFactor = notTrackedResetSpeedFactor * Time.deltaTime;
                    //キャリブ状態と同じ位置に顔を徐々に持っていく
                    CurrentAnalyzer.LerpToDefault(lerpFactor); 
                }
            }
        }

        private void OnDestroy()
        {
            Dispose();
            _dlibFaceAnalyzer.Dispose();
        }

        /// <summary>
        /// 顔トラッキング目的でカメラを起動します。
        /// </summary>
        /// <param name="cameraDeviceName"></param>
        /// <param name="trackingMode"></param>
        public void ActivateCameraForFaceTracking(string cameraDeviceName, FaceTrackingMode trackingMode)
        {
            Dispose();
            requestedDeviceName = cameraDeviceName;
            _trackingMode = trackingMode;
            CurrentAnalyzer.Start();
            Initialize();
        }

        public void StopCamera()
        {
            Dispose();
        }

        public void StartCalibration()
        {
            // NOTE: dlib用のカメラを起動してないときにのキャリブレーション要求は無視する。
            // このガードは、主に高負荷モードの顔トラッキング中に適用される
            if (HasInitDone)
            {
                _calibrationRequested = true;
            }
        }

        public void SetCalibrateData(string data)
        {
            try
            {
                if (string.IsNullOrEmpty(data))
                {
                    return;
                }

                var calibrationData = JsonUtility.FromJson<CalibrationData>(data);
                CalibrationData.faceSize = calibrationData.faceSize;
                CalibrationData.faceCenter = calibrationData.faceCenter;
                CalibrationData.pitchRateOffset = calibrationData.pitchRateOffset;
                CalibrationData.dnnPitchRateOffset = calibrationData.dnnPitchRateOffset;
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        
        private void Initialize()
        {
            if (!_isInitWaiting)
            {
                StartCoroutine(_Initialize());
            }
        }

        private IEnumerator _Initialize()
        {
            if (HasInitDone)
            {
                Dispose();
            }

            _isInitWaiting = true;
            
            // MediaPipe側でカメラを使っている場合、それの占有解除まで待つ
            while (!WebCamTextureOccupyStatusProvider.TryOccupyDlib())
            {
                yield return null;
            }

            // カメラの取得: 指定したのが無ければ諦める
            if (!string.IsNullOrEmpty(requestedDeviceName))
            {
                for (int i = 0; i < WebCamTexture.devices.Length; i++)
                {
                    if (WebCamTexture.devices[i].name == requestedDeviceName)
                    {
                        _webCamDevice = WebCamTexture.devices[i];
                        _webCamTexture = new WebCamTexture(
                            _webCamDevice.name,
                            rawTextureSize.x,
                            rawTextureSize.y,
                            requestedFps
                        );
                        break;
                    }
                }
            }

            if (_webCamTexture == null)
            {
                LogOutput.Instance.Write("Cannot find camera device " + requestedDeviceName + ".");
                _isInitWaiting = false;
                yield break;
            }

            // Starts the camera
            _webCamTexture.Play();

            while (_webCamTexture != null && !_webCamTexture.didUpdateThisFrame)
            {
                yield return null;
            }

            if (_webCamTexture == null)
            {
                //起動中にストップがかかってDisposeが呼ばれた場合はここに入る
                yield break;
            }

            _isInitWaiting = false;
            HasInitDone = true;
            _faceNotDetectedCountDown = activeButNotTrackedCount;

            if (_colors == null || _colors.Length != _webCamTexture.width * _webCamTexture.height)
            {
                _colors = new Color32[_webCamTexture.width * _webCamTexture.height];
            }
        }
        
        private void Dispose()
        {
            CurrentAnalyzer.Stop();

            _isInitWaiting = false;
            HasInitDone = false;
            FaceDetectedAtLeastOnce = false;

            if (_webCamTexture != null)
            {
                _webCamTexture.Stop();
                Destroy(_webCamTexture);
                _webCamTexture = null;
            }

            WebCamTextureOccupyStatusProvider.ReleaseDlib();
        }
    }
}