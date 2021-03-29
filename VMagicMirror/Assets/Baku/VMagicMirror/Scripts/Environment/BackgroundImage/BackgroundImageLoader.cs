using System.IO;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> 背景画像を適宜ロードするクラス </summary>
    public class BackgroundImageLoader : MonoBehaviour
    {
        [SerializeField] private BackgroundImageBoard board = null;
        
        private string _backgroundImagePath = "";

        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.SetBackgroundImagePath,
                command =>
                {
                    if (File.Exists(command.Content))
                    {
                        _backgroundImagePath = command.Content;
                        LoadBackgroundImage(command.Content);     
                    }
                    else
                    {
                        _backgroundImagePath = "";
                        ClearBackgroundImage();
                    }
                });

            receiver.AssignCommandHandler(
                VmmCommands.WindowFrameVisibility,
                command => CheckWindowFrameVisibility(command.ToBoolean())
            );
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

        private void CheckWindowFrameVisibility(bool isVisible) 
        {
            if (isVisible && File.Exists(_backgroundImagePath)) 
            { 
                LoadBackgroundImage(_backgroundImagePath);
            }
            else
            {
                ClearBackgroundImage();
            }        
        }
        
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
