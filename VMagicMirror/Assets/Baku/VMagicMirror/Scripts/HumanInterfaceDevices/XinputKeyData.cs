using XinputGamePad;

namespace Baku.VMagicMirror
{
    public struct XinputKeyData
    {
        public XinputKeyData(XinputKey key, bool isPressed) : this()
        {
            Key = key;
            IsPressed = isPressed;
        }

        public readonly XinputKey Key;
        public readonly bool IsPressed;

        public bool IsArrowKey =>
            Key == XinputKey.UP ||
            Key == XinputKey.RIGHT ||
            Key == XinputKey.DOWN ||
            Key == XinputKey.LEFT;
    }
}

