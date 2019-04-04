using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DlibFaceLandmarkDetector;
using System.Linq;

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

        public FaceParts FaceParts { get; } = new FaceParts();

        private WebCamTexture webCamTexture;
        private WebCamDevice webCamDevice;

        private Color32[] _colors;

        private bool isInitWaiting = false;
        private bool hasInitDone = false;

        private FaceLandmarkDetector faceLandmarkDetector;

        //TODO: textureは結果描画用のものなので最終的に消してよい
        private Texture2D texture;

        void Start()
        {
            string predictorFilePath = Path.Combine(Application.streamingAssetsPath, "sp_human_face_68.dat");

            if (!File.Exists(predictorFilePath))
            { 
                Debug.LogError("shape predictor file does not exist. Please copy from “DlibFaceLandmarkDetector/StreamingAssets/” to “Assets/StreamingAssets/” folder. ");
            }

            faceLandmarkDetector = new FaceLandmarkDetector(predictorFilePath);
            Initialize();
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
            if (hasInitDone)
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
                Debug.Log("Cannot find camera device " + requestedDeviceName + ".");
                isInitWaiting = false;
                yield break;
            }

            // Starts the camera
            webCamTexture.Play();

            while (!webCamTexture.didUpdateThisFrame)
            {
                yield return null;
            }

            isInitWaiting = false;
            hasInitDone = true;

            if (_colors == null || _colors.Length != webCamTexture.width * webCamTexture.height)
            {
                _colors = new Color32[webCamTexture.width * webCamTexture.height];
            }

            texture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            //GetComponent<Renderer>().material.mainTexture = texture;
            transform.localScale = new Vector3(texture.width, texture.height, 1);
        }

        private void Dispose()
        {
            isInitWaiting = false;
            hasInitDone = false;

            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                Destroy(webCamTexture);
                webCamTexture = null;
            }

            if (texture != null)
            {
                Destroy(texture);
                texture = null;
            }
        }

        void Update()
        {
            if (!hasInitDone ||
                !webCamTexture.isPlaying || 
                !webCamTexture.didUpdateThisFrame ||
                _colors == null
                )
            {
                return;
            }

            webCamTexture.GetPixels32(_colors);
            UpdateFaceParts(_colors);
        }

        private void UpdateFaceParts(Color32[] colors)
        {
            faceLandmarkDetector.SetImage(colors, texture.width, texture.height, 4, true);

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

            var landmarks = faceLandmarkDetector.DetectLandmark(mainPersonRect);
            FaceParts.Update(landmarks);

            //結果の描画: 基本的に要らないので禁止
            //faceLandmarkDetector.DrawDetectLandmarkResult(colors, texture.width, texture.height, 4, true, 0, 255, 0, 255);
            //faceLandmarkDetector.DrawDetectResult(colors, texture.width, texture.height, 4, true, 255, 0, 0, 255, 2);
            //texture.SetPixels32(colors);
            //texture.Apply(false);
        }

        void OnDestroy()
        {
            Dispose();
            faceLandmarkDetector?.Dispose();
        }
        
    }
}