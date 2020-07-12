using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror
{
    /// <summary> キーボードやマウスパッドの位置をユーザーが自由に編集できるようにするために表示するCanvasのUI層 </summary>
    public class DeviceTransformControlCanvas : MonoBehaviour
    {
        [SerializeField] private Canvas canvas = null;
        [SerializeField] private Toggle translateModeToggle = null;
        [SerializeField] private Toggle rotationModeToggle = null;
        [SerializeField] private Toggle scaleModeToggle = null;
        [SerializeField] private Toggle localCoordinateToggle = null;
        [SerializeField] private Toggle worldCoordinateToggle = null;
        [SerializeField] private Slider gamepadModelScaleSlider = null;
 
        //NOTE: こういう設計なのは、もともとreceiverが直にUIを触る挙動だったのを地続きで遅延初期化するようにいじくったため。
        public void Connect(DeviceTransformControlReceiver receiver)
        {
            receiver.RawCanvas = canvas;
            receiver.GamepadModelScaleSlider = gamepadModelScaleSlider;
            gamepadModelScaleSlider.onValueChanged.AddListener(receiver.GamepadScaleChanged);
            localCoordinateToggle.onValueChanged.AddListener(receiver.EnableLocalCoordinate);
            worldCoordinateToggle.onValueChanged.AddListener(receiver.EnableWorldCoordinate);
            translateModeToggle.onValueChanged.AddListener(receiver.EnableTranslateMode);
            rotationModeToggle.onValueChanged.AddListener(receiver.EnableRotateMode);
            scaleModeToggle.onValueChanged.AddListener(receiver.EnableScaleMode);
        }
    }
}
