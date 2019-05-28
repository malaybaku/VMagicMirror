using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using DlibFaceLandmarkDetector;

namespace Baku.VMagicMirror
{
    //NOTE: Dlib Face Landmark DetectorのExampleをいじりながら作成
    //最終的に消すもの
    // - 余計なpublic fieldぜんぶ
    // - 描画処理
    // - Windowサイズが変化したときしか効果発揮しなさそうな処理

    /// <summary>
    /// ウェブカメラを使ってVRMの表情として使える値をフィルタする
    /// </summary>
    public class FaceDetector : MonoBehaviour
    {
        //const string FaceTrackingDataFileName = "sp_human_face_68.dat";
        const string FaceTrackingDataFileName = "sp_human_face_68_for_mobile.dat";

        public string requestedDeviceName = null;
        public int requestedWidth = 320;
        public int requestedHeight = 240;
        public int requestedFPS = 30;
        public bool requestedIsFrontFacing = false;

        //実際に取得したWebCamTextureの幅がこれ以上(等値含む)の場合は画像処理に渡す前で画像を半分のサイズにする
        public int halfResizeWidthThreshold = 640;

        public FaceParts FaceParts { get; } = new FaceParts();

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

        private int TextureWidth =>
            (webCamTexture == null) ? requestedWidth :
            (webCamTexture.width >= halfResizeWidthThreshold) ? webCamTexture.width / 2 :
            webCamTexture.width;

        private int TextureHeight =>
            (webCamTexture == null) ? requestedHeight :
            (webCamTexture.width >= halfResizeWidthThreshold) ? webCamTexture.height / 2 :
            webCamTexture.height;

        private VRMBlink _nonTrackingBlink = null;
        private bool _calibrationRequested = false;

        private WebCamTexture webCamTexture;
        private WebCamDevice webCamDevice;

        private Color32[] _colors;
        private Color32[] _rawSizeColors;

        public bool HasInitDone { get; private set; } = false;
        private bool isInitWaiting = false;

        public bool FaceDetectedAtLeastOnce { get; private set; } = false;

        private FaceLandmarkDetector faceLandmarkDetector;

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


        void Start()
        {
            CalibrationData.SetDefaultValues();
            string predictorFilePath = Path.Combine(Application.streamingAssetsPath, FaceTrackingDataFileName);
            faceLandmarkDetector = new FaceLandmarkDetector(predictorFilePath);

            //顔検出を別スレッドに退避
            _cts = new CancellationTokenSource();
            Task.Run(FaceDetectionRoutine);
        }

        void Update()
        {
            //Update処理はランドマークの検出スレッドとの通信をやります
            // 1. データを送る準備が出来たら送る
            // 2. 結果が戻ってきてたら使う。キャリブが必要ならついでに実施

            if (HasInitDone &&
                webCamTexture.isPlaying &&
                webCamTexture.didUpdateThisFrame &&
                _colors != null &&
                !FaceDetectPrepared
                )
            {
                try
                {
                    if (webCamTexture.width >= halfResizeWidthThreshold)
                    {
                        webCamTexture.GetPixels32(_rawSizeColors);
                        SetHalfSizePixels(_rawSizeColors, _colors, webCamTexture.width, webCamTexture.height);
                        RequestUpdateFaceParts(_colors, TextureWidth, TextureHeight);
                        //UpdateFaceParts(_colors);
                    }
                    else
                    {
                        webCamTexture.GetPixels32(_colors);
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
        }

        void OnDestroy()
        {
            Dispose();
            faceLandmarkDetector?.Dispose();
            _cts?.Cancel();
        }

        public void StartCalibration()
        {
            _calibrationRequested = true;
        }

        public void SetCalibrateData(string data)
        {
            try
            {
                var calib = JsonUtility.FromJson<CalibrationData>(data);
                CalibrationData.eyeOpenHeight = calib.eyeOpenHeight;
                CalibrationData.eyeBrowPosition = calib.eyeBrowPosition;
                CalibrationData.noseHeight = calib.noseHeight;
                CalibrationData.faceCenter = calib.faceCenter;
                CalibrationData.faceSize = calib.faceSize;
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        public event EventHandler<CalibrationCompletedEventArgs> CalibrationCompleted;

        public void ActivateCamera(string cameraDeviceName)
        {
            requestedDeviceName = cameraDeviceName;
            Initialize();
        }

        public void StopCamera() => Dispose();

        public void SetNonCameraBlinkComponent(VRMBlink blink)
        {
            _nonTrackingBlink = blink;
            //カメラが使えてないなら自動まばたきに頑張ってもらう
            _nonTrackingBlink.enabled = !HasInitDone;
        }

        public void DisposeNonCameraBlinkComponent()
        {
            _nonTrackingBlink = null;
        }

        private void Initialize()
        {
            if (!isInitWaiting)
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

            isInitWaiting = true;

            // カメラの取得: 指定したのが無ければ諦める
            if (!string.IsNullOrEmpty(requestedDeviceName))
            {
                for (int i = 0; i < WebCamTexture.devices.Length; i++)
                {
                    if (WebCamTexture.devices[i].name == requestedDeviceName)
                    {
                        webCamDevice = WebCamTexture.devices[i];
                        webCamTexture = new WebCamTexture(webCamDevice.name, requestedWidth, requestedHeight, requestedFPS);
                        break;
                    }
                }
            }

            if (webCamTexture == null)
            {
                LogOutput.Instance.Write("Cannot find camera device " + requestedDeviceName + ".");
                isInitWaiting = false;
                yield break;
            }

            // Starts the camera
            webCamTexture.Play();

            while (webCamTexture != null && !webCamTexture.didUpdateThisFrame)
            {
                yield return null;
            }
            
            if (webCamTexture == null)
            {
                //起動中にストップがかかってDisposeが呼ばれた場合はここに入る
                yield break;
            }

            isInitWaiting = false;
            HasInitDone = true;
            if (_nonTrackingBlink != null)
            {
                _nonTrackingBlink.enabled = false;
            }

            if (_colors == null || _colors.Length != webCamTexture.width * webCamTexture.height)
            {
                if (webCamTexture.width >= halfResizeWidthThreshold)
                {
                    //画像が大きすぎるので、画像処理では0.5 x 0.5サイズを使う
                    _rawSizeColors = new Color32[webCamTexture.width * webCamTexture.height];
                    _colors = new Color32[(webCamTexture.width * webCamTexture.height) / 4];
                }
                else
                {
                    //そのまま使えばOK
                    _colors = new Color32[webCamTexture.width * webCamTexture.height];
                }
            }
        }

        private async void FaceDetectionRoutine()
        {
            while (!_cts.IsCancellationRequested)
            {
                await Task.Delay(16);
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

                faceLandmarkDetector.SetImage(_dlibInputColors, width, height, 4, true);

                List<Rect> faceRects = faceLandmarkDetector.Detect();
                //顔が取れないのでCompletedフラグは立てないままでOK
                if (faceRects == null || faceRects.Count == 0)
                {
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
                _mainPersonLandmarks = faceLandmarkDetector.DetectLandmark(mainPersonRect);

                FaceDetectCompleted = true;
            }
        }

        private void Dispose()
        {
            isInitWaiting = false;
            HasInitDone = false;
            if (_nonTrackingBlink != null)
            {
                _nonTrackingBlink.enabled = true;
            }

            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                Destroy(webCamTexture);
                webCamTexture = null;
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

            DetectedRect = new Rect(
                (mainPersonRect.xMin - TextureWidth / 2) / TextureWidth,
                -(mainPersonRect.yMax - TextureHeight / 2) / TextureWidth,
                mainPersonRect.width / TextureWidth,
                mainPersonRect.height / TextureWidth
                );

            FaceParts.Update(mainPersonRect, landmarks);

            FaceDetectedAtLeastOnce = true;
        }

        private void UpdateFaceParts(Color32[] colors)
        {
            faceLandmarkDetector.SetImage(colors, TextureWidth, TextureHeight, 4, true);

            List<Rect> faceRects = faceLandmarkDetector.Detect();
            if (faceRects == null || faceRects.Count == 0)
            {
                return;
            }

            Rect mainPersonRect = faceRects[0];

            //通常来ないが、複数人居たらいちばん大きく映っている人を採用
            if (faceRects.Count > 1)
            {
                mainPersonRect = faceRects
                    .OrderByDescending(r => r.width * r.height)
                    .First();
            }

            //Yは上下逆にしないと物理的なY方向にあわない点に注意
            DetectedRect = new Rect(
                (mainPersonRect.xMin - TextureWidth / 2) / TextureWidth,
                -(mainPersonRect.yMax - TextureHeight / 2) / TextureWidth,
                mainPersonRect.width / TextureWidth,
                mainPersonRect.height / TextureWidth
                );

            var landmarks = faceLandmarkDetector.DetectLandmark(mainPersonRect);
            FaceParts.Update(mainPersonRect, landmarks);
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


            string calibData = JsonUtility.ToJson(CalibrationData);
            CalibrationCompleted?.Invoke(this, new CalibrationCompletedEventArgs(calibData));
        }

        private void SetHalfSizePixels(Color32[] src, Color32[] dest, int srcWidth, int srcHeight)
        {
            //リサイズするうえで4ピクセルぶんを1ピクセルにまとめるので、
            // - k-NN: 4ピクセル中左上のピクセルの値をそのまま採用
            // - もう少しマシ: 4ピクセルの画素を平均(※通常ありえないが生テクスチャが奇数サイズのときの処理で要注意)
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
                    //NOTE: 周辺ピクセルを平均する事も考えられるが、重いわりに効果ないのでやらない
                }
            }
        }

        public class CalibrationCompletedEventArgs : EventArgs
        {
            public CalibrationCompletedEventArgs(string data)
            {
                Data = data;
            }
            public string Data { get; }
        }

    }
}