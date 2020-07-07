using System;
using System.Collections;
using System.IO;
using Baku.VMagicMirror.InterProcess;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class ScreenshotReceiver : MonoBehaviour
    {
        private const int ScreenshotCountSec = 3;
        
        //普段使ってるカメラ
        [SerializeField] private Camera normalMainCam = null;
        //スクショ専用の高解像度カメラ
        [SerializeField] private Camera screenShotCam = null;

        private ScreenshotCountDownCanvas _countDownCanvas = null;
        private float _screenshotCountDown = 0f;

        [Inject]
        public void Initialize(IMessageReceiver receiver, ScreenshotCountDownCanvas countDownCanvas)
        {
            _countDownCanvas = countDownCanvas;
            receiver.AssignCommandHandler(
                MessageCommandNames.TakeScreenshot,
                _ => StartScreenshotCountDown()
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.OpenScreenshotFolder,
                _ => OpenScreenshotFolder()
                );
        }

        private void Update()
        {
            if (_screenshotCountDown > 0)
            {
                _screenshotCountDown -= Time.deltaTime;
                if (_screenshotCountDown <= 0)
                {
                    _countDownCanvas.Hide();
                    TakeScreenshot();
                }
                _countDownCanvas.SetCount(Mathf.FloorToInt(_screenshotCountDown) + 1);
                _countDownCanvas.SetMod(Mathf.Repeat(_screenshotCountDown, 1.0f));
            }
        }
        
        private void StartScreenshotCountDown()
        {
            _countDownCanvas.Show();
            _countDownCanvas.SetCount(ScreenshotCountSec);
            _screenshotCountDown = ScreenshotCountSec;
        }
        
        private void TakeScreenshot()
        {
            //見た目の通りだが、
            // - 短辺が1080pixelを確実に超えるようにする
            // - 最低でも2倍解像度にする
            // - 最大でも4倍にする(大きすぎるのも重たくてアレなので)
            float windowSize = Mathf.Min(Screen.width, Screen.height);
            //例えばwindowSizeが500 = ウィンドウの短いほうが500pxなら3倍にする
            int superSize = Mathf.CeilToInt(1080 / windowSize);
            superSize = Mathf.Clamp(superSize, 2, 4);

            GetAndCreateScreenshotFolderPath();

            //この1フレームだけレンダリングする。重たいので普段は切っておくのがポイント
            SetupScreenshotCamera(superSize);
            screenShotCam.enabled = true;
            
            StartCoroutine(CaptureWithAlpha(
                Path.Combine(
                    GetAndCreateScreenshotFolderPath(),
                    DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png"
                )
            ));
        }

        private void SetupScreenshotCamera(int superSize)
        {
            screenShotCam.fieldOfView = normalMainCam.fieldOfView;
            screenShotCam.backgroundColor = normalMainCam.backgroundColor;
            screenShotCam.targetTexture = new RenderTexture(
                Screen.width * superSize,
                Screen.height * superSize,
                32,
                RenderTextureFormat.ARGB32
                );
        }

        private IEnumerator CaptureWithAlpha(string savePath)
        {
            yield return new WaitForEndOfFrame();

            var src = screenShotCam.targetTexture;
            var dst = new Texture2D(src.width, src.height, TextureFormat.ARGB32, false);
            var prevActive = RenderTexture.active;
            try
            {
                RenderTexture.active = src;
                dst.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
                File.WriteAllBytes(savePath, dst.EncodeToPNG());
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
            finally
            {
                RenderTexture.active = prevActive;
                Destroy(screenShotCam.targetTexture);
                screenShotCam.targetTexture = null;
                screenShotCam.enabled = false;
                Destroy(dst);
            }
        }

        private static void OpenScreenshotFolder() 
            => System.Diagnostics.Process.Start(GetAndCreateScreenshotFolderPath());

        //スクショ保存先フォルダのパスを生成する。このとき、フォルダが無ければ新規作成する
        private static string GetAndCreateScreenshotFolderPath()
        {
            //NOTE: エディタではdataPathがAssets/以下になって都合が悪いのでデスクトップに逃がす
            string path = Application.isEditor
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "Screenshots"
                    ) 
                : Path.Combine(
                    Path.GetDirectoryName(Application.dataPath),
                    "Screenshots"
                    );
                    
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

    }
}
