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
            // NOTE: Webカメラ高負荷トラッキングさえ有効ならばUpdateしちゃう…というスタイルもある。
            // 動作としてはトラッキングロス時にFace SwitchがリセットされてればOK
            if (!_facialValue.IsTracked)
            {
                return;
            }

            _faceSwitch.Update(_facialValue.BlendShapes);
        }
    }
}
