using System;
using System.Globalization;
using System.Windows.Data;

namespace Baku.VMagicMirrorConfig.View
{
    public class GameInputActionKeyToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not GameInputActionKey key)
            {
                return Binding.DoNothing;
            }

            // - customの場合は翻訳されない
            // - customの場合は"vrma_"みたいな接頭辞がついてるはずなので、ビルトインのものとは区別がつく想定
            switch (key.ActionType)
            {
                case GameInputButtonAction.None: return LocalizedString.GetString("GameInputKeyAssign_Action_None");
                case GameInputButtonAction.Jump: return LocalizedString.GetString("GameInputKeyAssign_ButtonAction_Jump");
                case GameInputButtonAction.Crouch: return LocalizedString.GetString("GameInputKeyAssign_ButtonAction_Crouch");
                case GameInputButtonAction.Run: return LocalizedString.GetString("GameInputKeyAssign_ButtonAction_Run");
                case GameInputButtonAction.Trigger: return LocalizedString.GetString("GameInputKeyAssign_ButtonAction_Trigger");
                case GameInputButtonAction.Punch: return LocalizedString.GetString("GameInputKeyAssign_ButtonAction_Punch");
                case GameInputButtonAction.Custom: return key.CustomActionKey;
            }

            //通常ここには到達しない
            return Binding.DoNothing;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
