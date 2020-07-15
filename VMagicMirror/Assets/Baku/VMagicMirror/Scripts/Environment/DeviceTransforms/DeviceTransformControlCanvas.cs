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
        public void Connect(DeviceTransformController controller)
        {
            controller.RawCanvas = canvas;
            controller.GamepadModelScaleSlider = gamepadModelScaleSlider;
            gamepadModelScaleSlider.onValueChanged.AddListener(controller.GamepadScaleChanged);
            localCoordinateToggle.onValueChanged.AddListener(controller.EnableLocalCoordinate);
            worldCoordinateToggle.onValueChanged.AddListener(controller.EnableWorldCoordinate);
            translateModeToggle.onValueChanged.AddListener(controller.EnableTranslateMode);
            rotationModeToggle.onValueChanged.AddListener(controller.EnableRotateMode);
            scaleModeToggle.onValueChanged.AddListener(controller.EnableScaleMode);
        }
    }
}
