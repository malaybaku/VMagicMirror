using UnityEngine;
using R3;

namespace Baku.VMagicMirror
{
    public sealed class CameraBackgroundColorController : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly CropAndOutlineController _cropAndOutlineController;
        private readonly Camera _camera;
        
        private bool _isTransparentBackground = false;
        private Color _latestNonTransparentBackgroundColor = Color.green;
        
        public CameraBackgroundColorController(
            IMessageReceiver receiver,
            CropAndOutlineController cropAndOutlineController,
            Camera camera)
        {
            _receiver = receiver;
            _cropAndOutlineController = cropAndOutlineController;
            _camera = camera;
        }
        
        public override void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.Chromakey,
                message =>
                {
                    var argb = message.ToColorFloats();
                    var a = argb[0];
                    _isTransparentBackground = a <= 0f;
                    _latestNonTransparentBackgroundColor = new Color(argb[1], argb[2], argb[3]);
                    
                    UpdateBackgroundColor();
                });
            
            _cropAndOutlineController.EnableCircleCrop
                .Skip(1)
                .Subscribe(_ => UpdateBackgroundColor())
                .AddTo(this);
        }

        public void ForceSetBackgroundTransparent()
        {
            _camera.backgroundColor = new Color(0, 0, 0, 0);
        }
        
        private void UpdateBackgroundColor()
        {
            // NOTE: 切り抜きがオンの場合、cameraの背景色ではなくPost Processで背景を切り落とすので、背景色は非透過のままにする
            if (_isTransparentBackground && !_cropAndOutlineController.EnableCircleCrop.CurrentValue)
            {
                _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }
            else
            {
                _camera.backgroundColor = _latestNonTransparentBackgroundColor;
            }
        }
    }
}
