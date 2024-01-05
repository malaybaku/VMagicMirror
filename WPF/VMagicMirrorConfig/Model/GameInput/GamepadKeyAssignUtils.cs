using System;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    internal static class GamepadKeyAssignUtils
    {
        /// <summary>
        /// 指定したカスタムアクション一覧に含まれないような割当のボタンがあると、trueを戻しつつ、対象ボタンの割当を「なし」にリセットします。
        /// この関数を呼んでもボタンの割当がまったく変化しなかった場合、falseを返します。
        /// </summary>
        /// <param name="target"></param>
        /// <param name="actionKeys"></param>
        /// <returns></returns>
        public static bool TryResetMissingCustomAction(this GameInputGamepadKeyAssign target, GameInputActionKey[] actionKeys)
        {
            var result = false;

            if (!actionKeys.Contains(target.ButtonAKey))
            {
                result = true;
                target.ButtonA = GameInputButtonAction.None;
                target.CustomButtonA.CustomKey = "";
            }
            if (!actionKeys.Contains(target.ButtonBKey))
            {
                result = true;
                target.ButtonB = GameInputButtonAction.None;
                target.CustomButtonB.CustomKey = "";
            }
            if (!actionKeys.Contains(target.ButtonXKey))
            {
                result = true;
                target.ButtonX = GameInputButtonAction.None;
                target.CustomButtonX.CustomKey = "";
            }
            if (!actionKeys.Contains(target.ButtonYKey))
            {
                result = true;
                target.ButtonY = GameInputButtonAction.None;
                target.CustomButtonY.CustomKey = "";
            }

            if (!actionKeys.Contains(target.ButtonLButtonKey))
            {
                result = true;
                target.ButtonLButton = GameInputButtonAction.None;
                target.CustomButtonLButton.CustomKey = "";
            }
            if (!actionKeys.Contains(target.ButtonYKey))
            {
                result = true;
                target.ButtonY = GameInputButtonAction.None;
                target.CustomButtonY.CustomKey = "";
            }
            if (!actionKeys.Contains(target.ButtonRButtonKey))
            {
                result = true;
                target.ButtonRButton = GameInputButtonAction.None;
                target.CustomButtonRButton.CustomKey = "";
            }
            if (!actionKeys.Contains(target.ButtonRTriggerKey))
            {
                result = true;
                target.ButtonRTrigger = GameInputButtonAction.None;
                target.CustomButtonRTrigger.CustomKey = "";
            }

            if (!actionKeys.Contains(target.ButtonMenuKey))
            {
                result = true;
                target.ButtonMenu = GameInputButtonAction.None;
                target.CustomButtonMenu.CustomKey = "";
            }
            if (!actionKeys.Contains(target.ButtonViewKey))
            {
                result = true;
                target.ButtonView = GameInputButtonAction.None;
                target.CustomButtonView.CustomKey = "";
            }

            return result;
        }
    }
}
