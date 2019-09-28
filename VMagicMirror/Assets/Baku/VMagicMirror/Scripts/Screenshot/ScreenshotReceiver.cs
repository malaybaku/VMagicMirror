using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    public class ScreenshotReceiver : MonoBehaviour
    {
        [Inject] private ReceivedMessageHandler handler = null;
        
        private void Start()
        {
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.TakeScreenshot:
                        TakeScreenshot();
                        break;
                    case MessageCommandNames.OpenScreenshotFolder:
                        OpenScreenshotFolder();
                        break;
                }
            });
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

            StartCoroutine(CaptureWithAlpha(
                superSize,
                Path.Combine(
                    GetAndCreateScreenshotFolderPath(),
                    DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png"
                )
            ));
        }

        private IEnumerator CaptureWithAlpha(int superSize, string savePath)
        {
            yield return new WaitForEndOfFrame();

            var texture = ScreenCapture.CaptureScreenshotAsTexture(superSize);
            var bytes = texture.EncodeToPNG();
            Destroy(texture);

            File.WriteAllBytes(savePath, bytes);
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
