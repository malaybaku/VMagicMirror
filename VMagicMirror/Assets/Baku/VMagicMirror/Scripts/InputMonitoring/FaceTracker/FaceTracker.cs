using System;
using System.Collections;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{ 
    /// <summary>
    /// ウェブカメラを使ってVRMの表情向けのトラッキングを回す
    /// </summary>
    public class FaceTracker : MonoBehaviour
    {
        private const string DlibFaceTrackingDataFileName = "sp_human_face_17.dat";

        [SerializeField] private string requestedDeviceName = null;
        [SerializeField] private bool isHighPowerMode = false;
        [SerializeField] private int requestedWidth = 320;
        [SerializeField] private int requestedHeight = 240;
        [SerializeField] private int requestedFps = 30;
        [SerializeField] private int halfResizeWidthThreshold = 640;
        [Tooltip("顔トラッキングがオンであるにも関わらず表情データが降ってこない状態が続き、異常値と判定するまでの秒数")]
        [SerializeField] private float activeButNotTrackedCount = 1.0f;
        [Tooltip("顔トラッキングが外れたとき、色々なパラメータを基準位置に戻すためのLerpファクター(これにTime.deltaTimeをかけた値を用いる")]
        [SerializeField] private float notTrackedResetSpeedFactor = 3.0f;
        [Tooltip("検出処理が走る最短間隔をミリ秒単位で規定します。")]
        [SerializeField] private int trackMinIntervalMillisec = 60;
        [SerializeField] private int trackMinIntervalMillisecOnHighPower = 40;
        
        /// <summary> キャリブレーションの内容 </summary>
        public CalibrationData CalibrationData { get; } = new CalibrationData();

        /// <summary> 顔検出スレッド上で、顔情報がアップデートされると発火します。 </summary>
        public event Action<FaceDetectionUpdateStatus> FaceDetectionUpdated;
        
        /// <summary> カメラが初期化済みかどうか </summary>
        public bool HasInitDone { get; private set; } = false;
        private bool _isInitWaiting = false;

        /// <summary> カメラを起動してから1度以上顔が検出されたかどうか </summary>
        public bool FaceDetectedAtLeastOnce { get; private set; } = false;

        private DlibFaceAnalyzeRoutine _dlibFaceAnalyzer;
        private DnnBasedFaceAnalyzeRoutine _dnnFaceAnalyzer;
        public FaceAnalyzeRoutineBase CurrentAnalyzer
        {
            get
            {
                if (IsHighPowerMode)
                {
                    return _dnnFaceAnalyzer;
                }
                else
                {
                    return _dlibFaceAnalyzer;
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
                _dnnFaceAnalyzer.DisableHorizontalFlip = value;
            } 
        }

        private int TrackMinIntervalMs =>
            isHighPowerMode ? trackMinIntervalMillisecOnHighPower : trackMinIntervalMillisec;

        private int TextureWidth =>
            (_webCamTexture == null) ? requestedWidth :
            (!isHighPowerMode && _webCamTexture.width >= halfResizeWidthThreshold) ? _webCamTexture.width / 2 :
            _webCamTexture.width;

        private int TextureHeight =>
            (_webCamTexture == null) ? requestedHeight :
            (!isHighPowerMode && _webCamTexture.width >= halfResizeWidthThreshold) ? _webCamTexture.height / 2 :
            _webCamTexture.height;

        private bool _calibrationRequested = false;
        private float _faceNotDetectedCountDown = 0.0f;

        private WebCamTexture _webCamTexture;
        private WebCamDevice _webCamDevice;

        private Color32[] _colors;
        private Color32[] _rawSizeColors;
        
        public bool IsHighPowerMode => isHighPowerMode;
        
        //UIスレッドがタイミングを見計らうために使う
        private float _countFromPreviousSetColors = 0f;
        private bool _hasFrameUpdateSincePreviousSetColors = false;

        private IMessageSender _sender;
        [Inject]
        public void Initialize(IMessageReceiver receiver, IMessageSender sender)
        {
            var _ = new FaceTrackerReceiver(receiver, this);
            //自分が自分のeventを購読するのもちょっと変ではあるが、sender変数を持たないでいいので若干スッキリする
            _sender = sender;
        }

        private void Start()
        {
            CalibrationData.SetDefaultValues();

            _dlibFaceAnalyzer = new DlibFaceAnalyzeRoutine(DlibFaceTrackingDataFileName);
            _dnnFaceAnalyzer = new DnnBasedFaceAnalyzeRoutine();
            //イベントを素通し
            _dlibFaceAnalyzer.FaceDetectionUpdated += FaceDetectionUpdated;
            _dnnFaceAnalyzer.FaceDetectionUpdated += FaceDetectionUpdated;
            
            _dlibFaceAnalyzer.SetUp();
            _dnnFaceAnalyzer.SetUp();
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
                   !CurrentAnalyzer.DetectPrepared;

                //どれか一つの条件が揃ってないのでダメ
                if (!canSetImage)
                {
                    return;
                }

                _countFromPreviousSetColors = 0f;
                _hasFrameUpdateSincePreviousSetColors = false;
                    
                try
                {
                    if (!isHighPowerMode && _webCamTexture.width >= halfResizeWidthThreshold)
                    {
                        _webCamTexture.GetPixels32(_rawSizeColors);
                        SetHalfSizePixels(_rawSizeColors, _colors, _webCamTexture.width, _webCamTexture.height);
                        CurrentAnalyzer.RequestNextProcess(_colors, TextureWidth, TextureHeight);
                    }
                    else
                    {
                        _webCamTexture.GetPixels32(_colors);
                        CurrentAnalyzer.RequestNextProcess(_colors, TextureWidth, TextureHeight);
                    }
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }
            
            //別スレッドの画像処理が終わっていたらその結果を受け取る
            void GetDetectionResult()
            {
                if (!CurrentAnalyzer.FaceDetectCompleted || !HasInitDone)
                {
                    return;
                }

                //NOTE: キャリブレーションをリクエストした場合、
                //このApplyによってCalibrationDataが書き換わるはずなので、それを通知する
                CurrentAnalyzer.ApplyFaceDetectionResult(CalibrationData, _calibrationRequested);
                if (_calibrationRequested)
                {
                    _calibrationRequested = false;
                    _sender.SendCommand(
                        MessageFactory.Instance.SetCalibrationFaceData(JsonUtility.ToJson(CalibrationData))
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
            _dnnFaceAnalyzer.Dispose();
            _dlibFaceAnalyzer.Dispose();
        }

        public void ActivateCamera(string cameraDeviceName, bool highPowerMode)
        {
            requestedDeviceName = cameraDeviceName;
            isHighPowerMode = highPowerMode;
            Initialize();
        }

        public void StopCamera() => Dispose();
        
        public void StartCalibration() => _calibrationRequested = true;

        public void SetCalibrateData(string data)
        {
            try
            {
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

            // カメラの取得: 指定したのが無ければ諦める
            if (!string.IsNullOrEmpty(requestedDeviceName))
            {
                for (int i = 0; i < WebCamTexture.devices.Length; i++)
                {
                    if (WebCamTexture.devices[i].name == requestedDeviceName)
                    {
                        _webCamDevice = WebCamTexture.devices[i];
                        int sizeFactor = isHighPowerMode ? 2 : 1;
                        _webCamTexture = new WebCamTexture(
                            _webCamDevice.name, 
                            requestedWidth * sizeFactor, 
                            requestedHeight * sizeFactor, 
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
                if (!isHighPowerMode && _webCamTexture.width >= halfResizeWidthThreshold)
                {
                    //画像が大きすぎるので、画像処理では0.5 x 0.5サイズを使う
                    _rawSizeColors = new Color32[_webCamTexture.width * _webCamTexture.height];
                    _colors = new Color32[(_webCamTexture.width * _webCamTexture.height) / 4];
                }
                else
                {
                    //そのまま使えばOK
                    _colors = new Color32[_webCamTexture.width * _webCamTexture.height];
                }
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
        }
        
        //画像をタテヨコ半分にする。(通常ありえないが)奇数サイズのピクセル数だった場合は切り捨て。
        private void SetHalfSizePixels(Color32[] src, Color32[] dest, int srcWidth, int srcHeight)
        {
            //この圧縮はk-NN: 4ピクセル中左上のピクセルの値をそのまま採用
            //周辺ピクセルを平均するのは重いわりに効果ないのでやらない
            int destWidth = srcWidth / 2;
            int destHeight = srcHeight / 2;
            for (int y = 0; y < destHeight; y++)
            {
                int destRowOffset = y * destWidth;
                int srcRowOffset = y * 2 * srcWidth;
                for (int x = 0; x < destWidth; x++)
                {
                    //左上ピクセルをそのまま使う
                    dest[destRowOffset + x] = src[srcRowOffset + x * 2];
                }
            }
        }
    }
}