namespace Baku.VMagicMirror
{
    public struct GamepadKeyData
    {
        public GamepadKeyData(GamepadKey key, bool isPressed) : this()
        {
            Key = key;
            IsPressed = isPressed;
        }

        public readonly GamepadKey Key;
        public readonly bool IsPressed;

        public bool IsArrowKey =>
            Key == GamepadKey.UP ||
            Key == GamepadKey.RIGHT ||
            Key == GamepadKey.DOWN ||
            Key == GamepadKey.LEFT;
    }
}

