using System.IO;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> 背景画像を適宜ロードするクラス </summary>
    public class BackgroundImageLoader : MonoBehaviour
    {
        [SerializeField] private BackgroundImageBoard board = null;

        private bool _windowFrameVisible = true;
        private string _backgroundImagePath = "";

        private bool WindowFrameVisible
        {
            get => _windowFrameVisible;
            set
            {
                if (_windowFrameVisible != value)
                {
                    _windowFrameVisible = value;
                    Refresh();
                }
            }
        }
        
        private string BackgroundImagePath
        {
            get => _backgroundImagePath;
            set
            {
                if (_backgroundImagePath == value)
                {
                    return;
                }

                //ちょっと普通のsetterなら許されない事だが、privateプロパティなので許す。
                _backgroundImagePath = File.Exists(value) ? value : "";
                Refresh();
            }
        }

        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.SetBackgroundImagePath,
                command => BackgroundImagePath = command.Content);

            receiver.AssignCommandHandler(
                VmmCommands.WindowFrameVisibility,
                command => WindowFrameVisible = command.ToBoolean());
        }

        private void Refresh()
        {
            if (_windowFrameVisible && File.Exists(_backgroundImagePath))
            {
                LoadBackgroundImage(_backgroundImagePath);
            }
            else
            {
                ClearBackgroundImage();
            }
        }

        private void LoadBackgroundImage(string imageFilePath)
        {
            var texture = LoadTexture(imageFilePath);
            if (texture != null)
            {
                board.SetImage(texture);
            }
            else
            {
                LogOutput.Instance.Write("Tried to load background image, but texture is invalid");
            }
        }
        
        private void ClearBackgroundImage() => board.DisposeImage();

        private static Texture2D LoadTexture(string filePath)
        {
            byte[] bin = File.ReadAllBytes(filePath);
            var result = new Texture2D(16, 16);
            if (!result.LoadImage(bin))
            {
                Destroy(result);
                return null;
            }
            return result;
        }
    }
}
