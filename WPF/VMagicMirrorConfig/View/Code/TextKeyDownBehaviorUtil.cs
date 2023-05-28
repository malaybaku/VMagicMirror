using System;
using System.Windows.Input;

namespace Baku.VMagicMirrorConfig.View
{
    static class TextKeyDownBehaviorUtil
    {
        public static void OnKeyDown(KeyEventArgs e, Action<Key> action, bool fireOnBasicModifierKeys = false)
        {
            if (e.Key == Key.Tab)
            {
                //タブはショートカットとしては認めず、ナビゲーション用の入力として流す
                return;
            }

            if (!fireOnBasicModifierKeys)
            {
                if (e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                    e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                    e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                    e.Key == Key.LWin || e.Key == Key.RWin)
                {
                    e.Handled = true;
                    return;
                }
            }

            //Lock系を含む、「さすがにそれは無いやろ」系のキーを無視
            if (e.Key == Key.NumLock || e.Key == Key.CapsLock || e.Key == Key.PrintScreen)
            {
                e.Handled = true;
                return;
            }

            action(e.Key);
            e.Handled = true;
        }
    }
}
