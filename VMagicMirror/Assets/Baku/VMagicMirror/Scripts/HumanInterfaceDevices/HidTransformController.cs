using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public class HidTransformController : MonoBehaviour
    {
        private const float TouchPadVerticalOffset = 0.01f;

        [SerializeField]
        private ReceivedMessageHandler handler = null;

        [SerializeField]
        private KeyboardProvider keyboard = null;

        [SerializeField]
        private TouchPadProvider touchpad = null;

        [SerializeField]
        private GamepadProvider gamepad = null;

        private Transform _keyboardRoot => keyboard.transform;
        private Transform _touchPadRoot => touchpad.transform.parent;
        private Transform _gamepadRoot => gamepad.transform;

        void Start()
        {
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.HidHeight:
                        SetHidHeight(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.HidHorizontalScale:
                        SetHidHorizontalScale(message.ParseAsPercentage());
                        break;
                    case MessageCommandNames.HidVisibility:
                        SetHidVisibility(message.ToBoolean());
                        break;

                    case MessageCommandNames.GamepadHeight:
                        SetGamepadHeight(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.GamepadHorizontalScale:
                        SetGamepadHorizontalScale(message.ParseAsPercentage());
                        break;
                    case MessageCommandNames.GamepadVisibility:
                        SetGamepadVisibility(message.ToBoolean());
                        break;

                    default:
                        break;
                }
            });
        }

        private void SetHidHeight(float v)
        {
            var keyboardPos = _keyboardRoot.position;
            _keyboardRoot.position = new Vector3(keyboardPos.x, v, keyboardPos.z);

            var touchPadPos = _touchPadRoot.position;
            _touchPadRoot.position = new Vector3(touchPadPos.x, v + TouchPadVerticalOffset, touchPadPos.z);
        }

        private void SetHidHorizontalScale(float v)
        {
            _keyboardRoot.localScale = new Vector3(v, 1.0f, v);
            _touchPadRoot.localScale = new Vector3(v, 1.0f, v);
        }

        private void SetHidVisibility(bool v)
        {
            keyboard.gameObject.SetActive(v);
            touchpad.gameObject.SetActive(v);
        }

        private void SetGamepadHeight(float v)
        {
            var gamepadPos = _gamepadRoot.position;
            _gamepadRoot.position = new Vector3(gamepadPos.x, v, gamepadPos.z);
        }

        private void SetGamepadHorizontalScale(float v)
        {
            _gamepadRoot.localScale = new Vector3(v, 1.0f, v);
        }

        private void SetGamepadVisibility(bool v)
        {
            _gamepadRoot.gameObject.SetActive(v);
        }
    }

}

