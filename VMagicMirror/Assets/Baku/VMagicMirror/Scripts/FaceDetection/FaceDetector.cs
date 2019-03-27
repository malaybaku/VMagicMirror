using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
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
            GetComponent<Renderer>().material.mainTexture = texture;
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
                !webCamTexture.didUpdateThisFrame
                )
            {
                return;
            }

            webCamTexture.GetPixels32(_colors);
            //同じ_colorsを画像検出のたびに使いまわしてOK
            Color32[] colors = _colors;
                
            if (colors != null)
            {

                faceLandmarkDetector.SetImage(colors, texture.width, texture.height, 4, true);

                //detect face rects
                List<Rect> detectResult = faceLandmarkDetector.Detect();

                //TODO: 68ランドマークの構成に基づいて眉の形とか目の開き具合をパラメータ化する

                foreach (var rect in detectResult)
                {
                    //Debug.Log ("face : " + rect);

                    //detect landmark points
                    faceLandmarkDetector.DetectLandmark(rect);

                    //draw landmark points
                    faceLandmarkDetector.DrawDetectLandmarkResult(colors, texture.width, texture.height, 4, true, 0, 255, 0, 255);
                }

                //draw face rect
                faceLandmarkDetector.DrawDetectResult(colors, texture.width, texture.height, 4, true, 255, 0, 0, 255, 2);

                texture.SetPixels32(colors);
                texture.Apply(false);
            }
        }

        void OnDestroy()
        {
            Dispose();
            faceLandmarkDetector?.Dispose();
        }
        
    }
}