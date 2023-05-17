using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Baku.VMagicMirror
{
    public class AntiAliasSettingSetter : PresenterBase
    {
        private readonly Camera _mainCamera;
        private readonly IMessageReceiver _receiver;
        private PostProcessLayer _postProcessLayer;
        
        public AntiAliasSettingSetter(Camera mainCamera, IMessageReceiver receiver)
        {
            _mainCamera = mainCamera;
            _receiver = receiver;
        }

        public override void Initialize()
        {
            _postProcessLayer = _mainCamera.GetComponent<PostProcessLayer>();
            _receiver.AssignCommandHandler(
                VmmCommands.SetAntiAliasStyle, 
                command => SetAntiAliasStyle(command.ToInt())
                );
        }
        
        private void SetAntiAliasStyle(int value)
        {
            if (value < 0 || value > (int)AntiAliasStyles.High)
            {
                return;
            }

            var style = (AntiAliasStyles)value;

            _postProcessLayer.antialiasingMode = style == AntiAliasStyles.None
                ? PostProcessLayer.Antialiasing.None
                : PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;

            _postProcessLayer.subpixelMorphologicalAntialiasing.quality = style switch
            {
                AntiAliasStyles.High => SubpixelMorphologicalAntialiasing.Quality.High,
                AntiAliasStyles.Mid => SubpixelMorphologicalAntialiasing.Quality.Medium,
                _ => SubpixelMorphologicalAntialiasing.Quality.Low
            };
        }
    }

    public enum AntiAliasStyles
    {
        None = 0,
        Low = 1,
        Mid = 2,
        High = 3,
    }
}
