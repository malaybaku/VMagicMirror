using System.Windows.Input;

namespace Baku.VMagicMirrorConfig
{
    public enum HotKeyActions : int
    {
        None = 0,
        SetCamera,
        CallWtm,
        ToggleAccessory,
        SetBodyMotionStyle,
        ToggleVMCPReceiveActive,
        ToggleKeyboardVisibility, // NOTE: このへんのToggle shortcutは、後から「Toggle/Hide/Show」みたいな下位分類をつけて拡張してもよい
        TogglePenVisibility,
        ToggleGamepadVisibility,
        ToggleShadowVisibility,
        ToggleOutlineVisibility,
        ToggleWindVisibility, // NOTE: Windに対してVisibilityという言い方をするのは「表示」タブにあるから
        EnableHandTracking,
        DisableHandTracking,
        ToggleHandTracking,
    }

    public enum HotKeyActionBodyMotionStyle : int
    {
        Default = 0,
        AlwaysHandDown = 1,
        GameInputLocomotion = 2,        
    }

    /// <summary>
    /// ホットキーを押した結果起こること
    /// </summary>
    /// <param name="Action"></param>
    /// <param name="ArgNumber"></param>
    /// <param name="ArgString"></param>
    public record HotKeyActionContent(HotKeyActions Action, int ArgNumber, string ArgString)
    {
        public static HotKeyActionContent Empty() => new(HotKeyActions.None, 0, "");
    }

    /// <summary>
    /// ホットキーの押し方と、押した結果起こることをセットにしたもの
    /// </summary>
    /// <param name="ModifierKeys"></param>
    /// <param name="Key"></param>
    /// <param name="ActionContent"></param>
    public record HotKeyRegisterItem(ModifierKeys ModifierKeys, Key Key, HotKeyActionContent ActionContent)
    {
        public static HotKeyRegisterItem Empty() 
            => new(ModifierKeys.None, Key.None, HotKeyActionContent.Empty());
    }

    public static class DefaultHotKeySetting
    {
        public static HotKeyRegisterItem[] Load()
        {
            var result = new HotKeyRegisterItem[13];

            for (int i = 0; i < 3; i++)
            {
                //Ctrl + Shift + (1|2|3) -> カメラアングル 1|2|3 を適用
                result[i] = new HotKeyRegisterItem(
                    ModifierKeys.Control | ModifierKeys.Shift, 
                    Key.D1 + i, 
                    new HotKeyActionContent(HotKeyActions.SetCamera, i + 1, "")
                    );
            }

            for (int i = 0; i < 10; i++)
            {
                //Ctrl + Alt + 1,2,... -> WtMの1,2,...,10個目のアイテムを実行
                result[i + 3] = new HotKeyRegisterItem(
                    ModifierKeys.Control | ModifierKeys.Alt,
                    Key.D0 + ((i + 1) % 10),
                    new HotKeyActionContent(HotKeyActions.CallWtm, i + 1, "")
                    );
            }

            return result;
        }
    }
}
