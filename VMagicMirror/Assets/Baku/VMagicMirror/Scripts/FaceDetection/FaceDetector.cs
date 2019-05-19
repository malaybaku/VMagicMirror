using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            (webCamTexture != null && webCamTexture.width >= halfResizeWidthThreshold) ?
            requestedWidth / 2 :
            requestedWidth;

        private int TextureHeight =>
            (webCamTexture != null && webCamTexture.width >= halfResizeWidthThreshold) ?
            requestedHeight / 2 :
            requestedHeight;

        private VRMBlink _nonTrackingBlink = null;
        private bool _calibrationRequested = false;

        private WebCamTexture webCamTexture;
        private WebCamDevice webCamDevice;

        private Color32[] _colors;
        private Color32[] _rawSizeColors;

        public bool HasInitDone { get; private set; } = false;
        private bool isInitWaiting = false;

        private FaceLandmarkDetector faceLandmarkDetector;

        //TODO: textureは結果描画用のものなので最終的に消してよい
        //private Texture2D texture;


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
            catch(Exception ex)
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

        public void StopCamera()
        {
            Dispose();
        }

        public void SetNonCameraBlinkComponent(VRMBlink blink)
        {
            _nonTrackingBlink = blink;
            //カメラが使えてないなら自動まばたきに頑張ってもらう、という意味
            _nonTrackingBlink.enabled = !HasInitDone;
        }

        public void DisposeNonCameraBlinkComponent()
        {
            _nonTrackingBlink = null;
        }

        void Start()
        {
            CalibrationData.SetDefaultValues();
            string predictorFilePath = Path.Combine(Application.streamingAssetsPath, "sp_human_face_68.dat");
            faceLandmarkDetector = new FaceLandmarkDetector(predictorFilePath);
        }

        void Update()
        {
            if (HasInitDone &&
                webCamTexture.isPlaying &&
                webCamTexture.didUpdateThisFrame &&
                _colors != null
                )
            {
                if (webCamTexture.width >= halfResizeWidthThreshold)
                {
                    webCamTexture.GetPixels32(_rawSizeColors);
                    SetHalfSizePixels(_rawSizeColors, _colors, webCamTexture.width, webCamTexture.height);
                    UpdateFaceParts(_colors);

                }
                else
                {
                    webCamTexture.GetPixels32(_colors);
                    UpdateFaceParts(_colors);
                }

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

            //texture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            //transform.localScale = new Vector3(texture.width, texture.height, 1);
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

            //if (texture != null)
            //{
            //    Destroy(texture);
            //    texture = null;
            //}
        }

        private void UpdateFaceParts(Color32[] colors)
        {
            //faceLandmarkDetector.SetImage(colors, texture.width, texture.height, 4, true);
            faceLandmarkDetector.SetImage(colors, TextureWidth, TextureHeight, 4, true);

            List<Rect> faceRects = faceLandmarkDetector.Detect();
            if (faceRects == null || faceRects.Count == 0)
            {
                return;
            }

            Rect mainPersonRect = faceRects[0];

            //Yは上下逆にしないと物理的なY方向にあわない点に注意
            DetectedRect = new Rect(
                (mainPersonRect.xMin - TextureWidth / 2) / TextureWidth,
                -(mainPersonRect.yMax - TextureHeight / 2) / TextureWidth,
                mainPersonRect.width / TextureWidth,
                mainPersonRect.height / TextureWidth
                );

            //通常来ないが、複数人居たらいちばん大きく映っている人を採用
            if (faceRects.Count > 1)
            {
                mainPersonRect = faceRects
                    .OrderByDescending(r => r.width * r.height)
                    .First();
            }

            var landmarks = faceLandmarkDetector.DetectLandmark(mainPersonRect);
            FaceParts.Update(mainPersonRect, landmarks);

            //結果の描画: 基本的に要らないので禁止
            //faceLandmarkDetector.DrawDetectLandmarkResult(colors, texture.width, texture.height, 4, true, 0, 255, 0, 255);
            //faceLandmarkDetector.DrawDetectResult(colors, texture.width, texture.height, 4, true, 255, 0, 0, 255, 2);
            //texture.SetPixels32(colors);
            //texture.Apply(false);
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
            //相当ヘンなカメラじゃないとこうはならないが…(4で割り切れないケースがほとんどありえない)
            //bool isEvenSize = (srcWidth % 2 == 0) && (srcHeight % 2 == 0);

            for (int y = 0; y < destHeight; y++)
            {
                int destRowOffset = y * destWidth;
                int srcRowOffset = y * 2 * srcWidth;
                //int nextSrcRowOffset = (y * 2 + 1) * srcWidth;
                for (int x = 0; x < destWidth; x++)
                {
                    //左上ピクセルをそのまま使う
                    dest[destRowOffset + x] = src[srcRowOffset + x * 2];

                    ////4ピクセル分の平均とる(これやらないとダメ…?)
                    //if (isEvenSize)
                    //{
                    //    //画面端の奇数要素に気を付けないといけないケース
                    //    Color32 leftUp = src[srcRowOffset + x * 2];
                    //    Color32 rightUp = (x * 2 + 1 < srcWidth) ? src[srcRowOffset + x * 2 + 1] : leftUp;
                    //    Color32 leftDown = (y * 2 + 1 < srcHeight) ? src[nextSrcRowOffset + x * 2] : leftUp;
                    //    Color32 rightDown =
                    //        (x * 2 + 1 < srcWidth) && (y * 2 + 1 < srcHeight) ?
                    //        src[nextSrcRowOffset + x * 2 + 1] :
                    //        leftUp;

                    //    dest[destRowOffset + x] = MeanColor(leftUp, rightUp, leftDown, rightDown);
                    //}
                    //else
                    //{
                    //    Color32 leftUp = src[srcRowOffset + x * 2];
                    //    Color32 rightUp = src[srcRowOffset + x * 2 + 1];
                    //    Color32 leftDown = src[nextSrcRowOffset + x * 2];
                    //    Color32 rightDown = src[nextSrcRowOffset + x * 2 + 1];
                    //    dest[destRowOffset + x] = MeanColor(leftUp, rightUp, leftDown, rightDown);
                    //}
                }
            }
        }

        private Color32 MeanColor(Color32 c1, Color32 c2, Color32 c3, Color32 c4)
        {
            //">> 2"は"/ 4"を思いつきで高速化したフリなので特に意味はないです
            return new Color32(
                (byte)((c1.r + c2.r + c3.r + c4.r) >> 2),
                (byte)((c1.g + c2.g + c3.g + c4.g) >> 2),
                (byte)((c1.b + c2.b + c3.b + c4.b) >> 2),
                (byte)((c1.a + c2.a + c3.a + c4.a) >> 2)
                );
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