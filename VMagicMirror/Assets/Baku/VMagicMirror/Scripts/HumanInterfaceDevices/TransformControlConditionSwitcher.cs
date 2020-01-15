using UnityEngine;
using mattatz.TransformControl;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// TransformControlが実際に動く条件を決定するクラス
    /// </summary>
    [RequireComponent(typeof(DeviceTransformControlReceiver))]
    public class TransformControlConditionSwitcher : MonoBehaviour
    {

        private DeviceTransformControlReceiver _receiver;
        
        private TransformControl _keyboardControl = null;
        private TransformControl _touchPadControl = null;
        private TransformControl _gamePadControl = null;
        private TransformControl _midiControl = null;


        private KeyboardVisibility _keyboardVisibility;
        private TouchpadVisibility _touchPadVisibility;
        private GamepadVisibilityReceiver _gamepadVisibility;
        private MidiControllerVisibility _midiControllerVisibility;
        
        private void Start()
        {
            _receiver = GetComponent<DeviceTransformControlReceiver>();

            _keyboardControl = _receiver.KeyboardControl;
            _touchPadControl = _receiver.TouchPadControl;
            _gamePadControl = _receiver.GamepadControl;
            _midiControl = _receiver.MidiControl;

            _keyboardVisibility = _keyboardControl.GetComponent<KeyboardVisibility>();
            _touchPadVisibility =  _touchPadControl.GetComponent<TouchpadVisibility>();
            _gamepadVisibility = _gamePadControl.GetComponent<GamepadVisibilityReceiver>();
            _midiControllerVisibility = _midiControl.GetComponent<MidiControllerVisibility>();
        }

        private void Update()
        {
            if (!_receiver.IsDeviceFreeLayoutEnabled)
            {
                return;
            }

            _receiver.CanShowKeyboardControl = _keyboardVisibility.IsVisible;
            _receiver.CanShowTouchpadControl = _touchPadVisibility.IsVisible;
            _receiver.CanShowGamepadControl = _gamepadVisibility.IsVisible;
            _receiver.CanShowMidiControl = _midiControllerVisibility.IsVisible;
        }
    }
}
