using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using DlibFaceLandmarkDetector;
using Zenject;

namespace Baku.VMagicMirror
{ 
    /// <summary>
    /// ウェブカメラを使ってVRMの表情向けのトラッキングを回す
    /// </summary>
    public class FaceTracker : MonoBehaviour
    {
        //const string FaceTrackingDataFileName = "sp_human_face_68.dat";
        const string FaceTrackingDataFileName = "sp_human_face_68_for_mobile.dat";

        [SerializeField]
        private string requestedDeviceName = null;
        
        [SerializeField]
        private int requestedWidth = 320;
        
        [SerializeField]
        private int requestedHeight = 240;
        
        [SerializeField]
        private int requestedFps = 30;
        
        [SerializeField]
        private int halfResizeWidthThreshold = 640;

        [Tooltip("顔トラッキングがオンであるにも関わらず表情データが降ってこない状態が続き、異常値と判定するまでの秒数")]
        [SerializeField] private float activeButNotTrackedCount = 1.0f;
        
        [Tooltip("顔トラッキングが外れたとき、色々なパラメータを基準位置に戻すためのLerpファクター(これにTime.deltaTimeをかけた値を用いる")]
        [SerializeField] private float notTrackedResetSpeedFactor = 3.0f;
        
        /// <summary> 検出した顔パーツそれぞれ </summary>
        public FaceParts FaceParts { get; } = new FaceParts();

        /// <summary> キャリブレーションの内容 </summary>
        public CalibrationData CalibrationData { get; } = new CalibrationData();

        /// <summary>
        /// 検出した顔の範囲を正規化した範囲で返す。
        /// </summary>
        /// <remarks>
        /// 範囲はX座標が[-.5,.5]になるよう正規化する。
        /// Y座標はX座標を正規化した係数に、アスペクト比が変化しないようにファクターを掛けた値で正規化する。
        /// (通常カメラは横に長いからYの値域は[-.5, .5]より小さい。)
        /// Yは上がプラス方向。
        /// Xは人が右に行くとプラス方向。これはカメラ座標と反転した値である。
        /// </remarks>
        public Rect DetectedRect { get; private set; }

        /// <summary>
        /// 顔検出スレッド上で、顔情報がアップデートされると発火します。
        /// </summary>
        public event Action<FaceDetectionUpdateStatus> FaceDetectionUpdated;

        /// <summary> カメラが初期化済みかどうか </summary>
        public bool HasInitDone { get; private set; } = false;
        private bool _isInitWaiting = false;

        /// <summary> カメラを起動してから1度以上顔が検出されたかどうか </summary>
        public bool FaceDetectedAtLeastOnce { get; private set; } = false;

        public bool DisableHorizontalFlip
        {
            get => FaceParts.DisableHorizontalFlip;
            set => FaceParts.DisableHorizontalFlip = value;
        }

        private int TextureWidth =>
            (_webCamTexture == null) ? requestedWidth :
            (_webCamTexture.width >= halfResizeWidthThreshold) ? _webCamTexture.width / 2 :
            _webCamTexture.width;

        private int TextureHeight =>
            (_webCamTexture == null) ? requestedHeight :
            (_webCamTexture.width >= halfResizeWidthThreshold) ? _webCamTexture.height / 2 :
            _webCamTexture.height;

        private bool _calibrationRequested = false;
        private float _faceNotDetectedCountDown = 0.0f;

        private WebCamTexture _webCamTexture;
        private WebCamDevice _webCamDevice;

        private Color32[] _colors;
        private Color32[] _rawSizeColors;

        private FaceLandmarkDetector _faceLandmarkDetector;

        #region Multi Thread Face Detection

        private CancellationTokenSource _cts = null;
        private readonly object _faceDetectPreparedLock = new object();
        private bool _faceDetectPrepared = false;
        private bool FaceDetectPrepared
        {
            get { lock (_faceDetectPreparedLock) return _faceDetectPrepared; }
            set { lock (_faceDetectPreparedLock) _faceDetectPrepared = value; }
        }

        private readonly object _faceDetectCompletedLock = new object();
        private bool _faceDetectCompleted = false;
        private bool FaceDetectCompleted
        {
            get { lock (_faceDetectCompletedLock) return _faceDetectCompleted; }
            set { lock (_faceDetectCompletedLock) _faceDetectCompleted = value; }
        }
        
        //UIスレッドが書き込み、Dlibの呼び出しスレッドが読み込む
        private Color32[] _inputColors = null;
        private int _inputWidth = 0;
        private int _inputHeight = 0;

        //DLibのスレッドだけが使う
        private Color32[] _dlibInputColors = null;

        //Dlibの呼び出しスレッドが書き込み、UIスレッドが読み込む
        private Rect _mainPersonRect;
        private List<Vector2> _mainPersonLandmarks = null;

        #endregion

        [Inject]
        public void Initialize(IMessageReceiver receiver, IMessageSender sender)
        {
            var _ = new FaceTrackerReceiver(receiver, this);
            var __ = new CalibrationCompletedDataSender(sender, this);
        }
        
        private void Start()
        {
            CalibrationData.SetDefaultValues();
            string predictorFilePath = Path.Combine(Application.streamingAssetsPath, FaceTrackingDataFileName);
            _faceLandmarkDetector = new FaceLandmarkDetector(predictorFilePath);

            //顔検出を別スレッドに退避
            _cts = new CancellationTokenSource();
            Task.Run(FaceDetectionRoutine);
        }

        private void Update()
        {
            //Update処理はランドマークの検出スレッドとの通信をやります
            // 1. データを送る準備が出来たら送る
            // 2. 結果が戻ってきてたら使う。キャリブが必要ならついでに実施

            if (HasInitDone &&
                _webCamTexture.isPlaying &&
                _webCamTexture.didUpdateThisFrame &&
                _colors != null &&
                !FaceDetectPrepared
                )
            {
                try
                {
                    if (_webCamTexture.width >= halfResizeWidthThreshold)
                    {
                        _webCamTexture.GetPixels32(_rawSizeColors);
                        SetHalfSizePixels(_rawSizeColors, _colors, _webCamTexture.width, _webCamTexture.height);
                        RequestUpdateFaceParts(_colors, TextureWidth, TextureHeight);
                        //UpdateFaceParts(_colors);
                    }
                    else
                    {
                        _webCamTexture.GetPixels32(_colors);
                        RequestUpdateFaceParts(_colors, TextureWidth, TextureHeight);
                        //UpdateFaceParts(_colors);
                    }
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }

            if (FaceDetectCompleted)
            {
                GetFaceDetectResult();
                if (_calibrationRequested)
                {
                    _calibrationRequested = false;
                    UpdateCalibrationData();
                }
            }

            //顔トラ起動中はつねに「実は顔トラッキング出来てないのでは？」というのをチェックする
            if (HasInitDone)
            {
                if (_faceNotDetectedCountDown >= 0)
                {
                    _faceNotDetectedCountDown -= Time.deltaTime;
                }
                
                if (_faceNotDetectedCountDown < 0)
                {
                    //顔トラが全然更新されない -> ヘンなとこで固まってる可能性が高いので、正面姿勢に戻って頂く
                    LerpToDefaultFace();
                }
            }
        }

        private void OnDestroy()
        {
            Dispose();
            _faceLandmarkDetector?.Dispose();
            _cts?.Cancel();
        }

        public void ActivateCamera(string cameraDeviceName)
        {
            requestedDeviceName = cameraDeviceName;
            Initialize();
        }

        public void StopCamera() => Dispose();
        
        public void StartCalibration()
        {
            _calibrationRequested = true;
        }

        public void SetCalibrateData(string data)
        {
            try
            {
                var calibrationData = JsonUtility.FromJson<CalibrationData>(data);
                CalibrationData.eyeOpenHeight = calibrationData.eyeOpenHeight;
                CalibrationData.eyeBrowPosition = calibrationData.eyeBrowPosition;
                CalibrationData.noseHeight = calibrationData.noseHeight;
                CalibrationData.faceCenter = calibrationData.faceCenter;
                CalibrationData.faceSize = calibrationData.faceSize;
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        public event Action<string> CalibrationCompleted;

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
                        _webCamTexture = new WebCamTexture(_webCamDevice.name, requestedWidth, requestedHeight, requestedFps);
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
                if (_webCamTexture.width >= halfResizeWidthThreshold)
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

        private void FaceDetectionRoutine()
        {
            while (!_cts.IsCancellationRequested)
            {
                Thread.Sleep(16);
                //書いてある通りだが、UIスレッド側からテクスチャが来ない場合や、
                //テクスチャはあるが出力読み出し待ちになってる場合は無視
                if (!FaceDetectPrepared || FaceDetectCompleted)
                {
                    continue;
                }

                int width = _inputWidth;
                int height = _inputHeight;
                if (_dlibInputColors == null || 
                    _dlibInputColors.Length != _inputColors.Length
                    )
                {
                    _dlibInputColors = new Color32[_inputColors.Length];
                }
                Array.Copy(_inputColors, _dlibInputColors, _inputColors.Length);

                //この時点で入力データを抜き終わっているので、次のデータがあればセットしても大丈夫
                FaceDetectPrepared = false;

                _faceLandmarkDetector.SetImage(_dlibInputColors, width, height, 4, true);

                List<Rect> faceRects = _faceLandmarkDetector.Detect();
                //顔が取れないのでCompletedフラグは立てないままでOK
                if (faceRects == null || faceRects.Count == 0)
                {
                    RaiseFaceDetectionUpdate(new FaceDetectionUpdateStatus()
                    {
                        Image = _dlibInputColors,
                        Width = width,
                        Height = height,
                        HasValidFaceArea = false,
                    });
                    continue;
                }

                Rect mainPersonRect = faceRects[0];
                //通常来ないが、複数人居たらいちばん大きく映っている人を採用
                if (faceRects.Count > 1)
                {
                    mainPersonRect = faceRects
                        .OrderByDescending(r => r.width * r.height)
                        .First();
                }
                _mainPersonRect = mainPersonRect;
                _mainPersonLandmarks = _faceLandmarkDetector.DetectLandmark(mainPersonRect);
                
                RaiseFaceDetectionUpdate(new FaceDetectionUpdateStatus()
                {
                    Image = _dlibInputColors,
                    Width = width,
                    Height = height,
                    HasValidFaceArea = true,
                    FaceArea = mainPersonRect,
                });

                FaceDetectCompleted = true;
            }
        }

        private void Dispose()
        {
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

        private void RequestUpdateFaceParts(Color32[] colors, int width, int height)
        {
            if (_inputColors == null || _inputColors.Length != colors.Length)
            {
                _inputColors = new Color32[colors.Length];
            }
            Array.Copy(colors, _inputColors, colors.Length);
            _inputWidth = width;
            _inputHeight = height;
            FaceDetectPrepared = true;
        }

        private void GetFaceDetectResult()
        {
            var mainPersonRect = _mainPersonRect;
            //特徴点リストは参照ごと受け取ってOK(Completedフラグが降りるまでは競合しない)
            var landmarks = _mainPersonLandmarks;
            _mainPersonLandmarks = null;

            //出力を拾い終わった時点で次の処理に入ってもらって大丈夫
            FaceDetectCompleted = false;

            float x = (mainPersonRect.xMin - TextureWidth / 2) / TextureWidth;
            if (DisableHorizontalFlip)
            {
                x = -x;
            }
            
            DetectedRect = new Rect(
                x,
                -(mainPersonRect.yMax - TextureHeight / 2) / TextureWidth,
                mainPersonRect.width / TextureWidth,
                mainPersonRect.height / TextureWidth
                );

            FaceParts.Update(mainPersonRect, landmarks);

            FaceDetectedAtLeastOnce = true;
            //顔が検出できてるので、未検出カウントダウンは最初からやり直し
            _faceNotDetectedCountDown = activeButNotTrackedCount;
        }

        private void LerpToDefaultFace()
        {
            float lerpFactor = notTrackedResetSpeedFactor * Time.deltaTime;

            //キャリブ状態と同じ位置に顔を徐々に持っていく
            var center = Vector2.Lerp(DetectedRect.center, CalibrationData.faceCenter, lerpFactor);
            float edgeLen = Mathf.Sqrt(CalibrationData.faceSize);
            var size = Vector2.Lerp(DetectedRect.size, new Vector2(edgeLen, edgeLen), lerpFactor);
            //NOTE: centerじゃなくてbottom leftを指定するんですね…はい。
            DetectedRect = new Rect(center - 0.5f * size, size);
            
            FaceParts.LerpToDefault(CalibrationData, lerpFactor);
        }
        
        private void UpdateCalibrationData()
        {
            CalibrationData.faceSize = DetectedRect.width * DetectedRect.height;
            CalibrationData.faceCenter = DetectedRect.center;

            CalibrationData.eyeOpenHeight = 0.5f * (
                FaceParts.LeftEye.CurrentEyeOpenValue +
                FaceParts.RightEye.CurrentEyeOpenValue
                );

            CalibrationData.eyeBrowPosition = 0.5f * (
                FaceParts.LeftEyebrow.CurrentHeight + 
                FaceParts.RightEyebrow.CurrentHeight
                );

            CalibrationData.noseHeight =
                FaceParts.Nose.CurrentNoseBaseHeightValue;


            CalibrationCompleted?.Invoke(JsonUtility.ToJson(CalibrationData));
        }
        
        //画像をタテヨコ半分にする。(通常ありえないが)奇数サイズのピクセル数だった場合は切り捨て。
        private void SetHalfSizePixels(Color32[] src, Color32[] dest, int srcWidth, int srcHeight)
        {
            //この圧縮はk-NN: 4ピクセル中左上のピクセルの値をそのまま採用
            //周辺ピクセルを平均するのは事も考えられるが、重いわりに効果ないのでやらない
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

        private void RaiseFaceDetectionUpdate(FaceDetectionUpdateStatus status)
        {
            try
            {
                FaceDetectionUpdated?.Invoke(status);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
    }

    public struct FaceDetectionUpdateStatus
    {
        //public WebCamTexture WebCam { get; set; }
        public Color32[] Image { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool HasValidFaceArea { get; set; }
        public Rect FaceArea { get; set; }
    }
}