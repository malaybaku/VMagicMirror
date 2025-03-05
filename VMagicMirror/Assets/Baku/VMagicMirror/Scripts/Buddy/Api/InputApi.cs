using System;
using Baku.VMagicMirror.Buddy.Api.Interface;

namespace Baku.VMagicMirror.Buddy.Api
{
    public class InputApi : IInputApi
    {
        private readonly InputApiImplement _impl;
        private Vector2 _gamepadLeftStick;
        private Vector2 _gamepadRightStick;

        public InputApi(InputApiImplement impl)
        {
            _impl = impl;
        }
        
        internal void InvokeKeyboardKeyDown(string key) => KeyboardKeyDown?.Invoke(key);
        internal void InvokeKeyboardKeyUp(string key) => KeyboardKeyUp?.Invoke(key);
        internal void InvokeGamepadButtonDown(GamepadKey key) => GamepadButtonDown?.Invoke(key.ToApiValue());
        internal void InvokeGamepadButtonUp(GamepadKey key) => GamepadButtonUp?.Invoke(key.ToApiValue());

        public event Action<GamepadButton> GamepadButtonDown;
        public event Action<GamepadButton> GamepadButtonUp;
        public event Action<string> KeyboardKeyDown;
        public event Action<string> KeyboardKeyUp;

        public Vector2 MousePosition => _impl.GetNonDimensionalMousePosition().ToApiValue();

        public Vector2 GamepadLeftStick => _impl.GetGamepadLeftStickPosition().ToApiValue();

        public Vector2 GamepadRightStick => _impl.GetGamepadRightStickPosition().ToApiValue();

        public bool GetGamepadButton(GamepadButton key) => _impl.GetGamepadButton(key.ToEngineValue());
    }
}

