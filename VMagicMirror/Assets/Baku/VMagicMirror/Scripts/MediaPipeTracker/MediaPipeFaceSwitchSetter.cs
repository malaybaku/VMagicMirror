using Baku.VMagicMirror.ExternalTracker;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class MediaPipeFaceSwitchSetter : ITickable
    {
        private readonly MediaPipeFacialValueRepository _facialValue;
        private readonly FaceSwitchExtractor _faceSwitch;
 
        [Inject]
        public MediaPipeFaceSwitchSetter(MediaPipeFacialValueRepository facialValue, FaceSwitchExtractor faceSwitch)
        {
            _facialValue = facialValue;
            _faceSwitch = faceSwitch;
        }

        void ITickable.Tick()
        {
            // NOTE: FaceSwitchのオフじゃなくて無視のみで大丈夫か、という問題はある…かも。
            if (!_facialValue.IsTracked)
            {
                return;
            }

            _faceSwitch.Update(_facialValue.BlendShapes);
        }
    }
}
