using System.IO;
using R3;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary> 背景画像を適宜ロードするクラス </summary>
    public class BackgroundImageLoader : MonoBehaviour
    {
        [SerializeField] private BackgroundImageBoard board = null;

        private readonly ReactiveProperty<bool> _windowFrameVisible = new(true);
        private string _backgroundImagePath = "";

        private CropAndOutlineController _cropAndOutlineController;
        
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
        public void Initialize(IMessageReceiver receiver, CropAndOutlineController cropAndOutlineController)
        {
            _cropAndOutlineController = cropAndOutlineController;

            receiver.AssignCommandHandler(
                VmmCommands.SetBackgroundImagePath,
                command => BackgroundImagePath = command.GetStringValue());

            receiver.BindBoolProperty(VmmCommands.WindowFrameVisibility, _windowFrameVisible);
            _windowFrameVisible
                .CombineLatest(cropAndOutlineController.EnableCircleCrop, (x, y) => Unit.Default)
                .Subscribe(_ => Refresh())
                .AddTo(this);
        }

        private void Refresh()
        {
            if ((_windowFrameVisible.CurrentValue || _cropAndOutlineController.EnableCircleCrop.CurrentValue) && 
                File.Exists(_backgroundImagePath))
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
