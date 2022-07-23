using System.Windows.Input;

namespace Baku.VMagicMirrorConfig
{
    public enum HotKeyActions : int
    {
        None,
        SetCamera,
        CallWtm,
        //TODO: ToggleAccessoryという項目もつけたいが、アクセサリの仕様上ちょっとムズいので一旦無し
    }

    /// <summary>
    /// ホットキーを押した結果起こること
    /// </summary>
    /// <param name="Action"></param>
    /// <param name="ArgNumber"></param>
    /// <param name="ArgContent"></param>
    public record HotKeyActionContent(HotKeyActions Action, int ArgNumber)
    {
        public static HotKeyActionContent Empty() => new(HotKeyActions.None, 0);
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
                    new HotKeyActionContent(HotKeyActions.SetCamera, i + 1)
                    );
            }

            for (int i = 0; i < 10; i++)
            {
                //Ctrl + Alt + num -> WtMの0,1,...,9個目のアイテムを実行
                result[i + 3] = new HotKeyRegisterItem(
                    ModifierKeys.Control | ModifierKeys.Alt,
                    Key.D1 + i,
                    new HotKeyActionContent(HotKeyActions.CallWtm, i)
                    );
            }

            return result;
        }
    }
}
