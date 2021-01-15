using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    //TODO: このクラスは設計に問題抱えてそう…なんだけど一瞬で治る問題ではないのが何とも。

    /// <summary> モーション関係で、操作ではなく設定値を受け取るレシーバクラス </summary>
    public class MotionSettingReceiver : MonoBehaviour
    {
        //Word to Motionの専用入力に使うデバイスを指定する定数値
//        private const int DeviceTypeNone = -1;
        private const int DeviceTypeKeyboardWord = 0;
        private const int DeviceTypeGamepad = 1;
        private const int DeviceTypeKeyboardTenKey = 2;
        private const int DeviceTypeMidiController = 3;
        
        [SerializeField] private GamepadBasedBodyLean gamePadBasedBodyLean = null;
        [SerializeField] private HandIKIntegrator handIkIntegrator = null;
        [SerializeField] private HeadIkIntegrator headIkIntegrator = null;

        private GamepadHandIKGenerator GamepadHandIk => handIkIntegrator.GamepadHand;
        
        private XInputGamePad _gamePad = null;

        [Inject]
        public void Initialize(IMessageReceiver receiver, XInputGamePad gamePad)
        {
            _gamePad = gamePad;
            receiver.AssignCommandHandler(
                VmmCommands.EnableHidArmMotion,
                message => handIkIntegrator.EnableHidArmMotion = message.ToBoolean()
                );
            receiver.AssignCommandHandler(
                VmmCommands.EnableNoHandTrackMode,
                message => handIkIntegrator.AlwaysHandDownMode = message.ToBoolean()
                );
            receiver.AssignCommandHandler(
                VmmCommands.EnableTypingHandDownTimeout,
                message => handIkIntegrator.EnableHandDownTimeout = message.ToBoolean()
                );
            receiver.AssignCommandHandler(
                VmmCommands.LengthFromWristToTip,
                message => SetLengthFromWristToTip(message.ParseAsCentimeter())
                );
            receiver.AssignCommandHandler(
                VmmCommands.HandYOffsetBasic,
                message => SetHandYOffsetBasic(message.ParseAsCentimeter())
                );
            receiver.AssignCommandHandler(
                VmmCommands.HandYOffsetAfterKeyDown,
                message => SetHandYOffsetAfterKeyDown(message.ParseAsCentimeter())
                );
            receiver.AssignCommandHandler(
                VmmCommands.EnablePresenterMotion,
                message => handIkIntegrator.EnablePresentationMode = message.ToBoolean()
                );
            receiver.AssignCommandHandler(
                VmmCommands.PresentationArmRadiusMin,
                message =>
                    handIkIntegrator.Presentation.PresentationArmRadiusMin = message.ParseAsCentimeter()
                );
            receiver.AssignCommandHandler(
                VmmCommands.LookAtStyle,
                message => headIkIntegrator.SetLookAtStyle(message.Content)
                );
            receiver.AssignCommandHandler(
                VmmCommands.EnableGamepad,
                message => _gamePad.SetEnableGamepad(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.PreferDirectInputGamepad,
                message => _gamePad.SetPreferDirectInputGamepad(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.GamepadLeanMode,
                message =>
                {
                    gamePadBasedBodyLean.SetGamepadLeanMode(message.Content);
                    GamepadHandIk.SetGamepadLeanMode(message.Content);
                });
            receiver.AssignCommandHandler(
                VmmCommands.GamepadLeanReverseHorizontal,
                message =>
                {
                    gamePadBasedBodyLean.ReverseGamepadStickLeanHorizontal = message.ToBoolean();
                    GamepadHandIk.ReverseGamepadStickLeanHorizontal = message.ToBoolean();
                });
            receiver.AssignCommandHandler(
                VmmCommands.GamepadLeanReverseVertical,
                message =>
                {
                    gamePadBasedBodyLean.ReverseGamepadStickLeanVertical = message.ToBoolean();
                    GamepadHandIk.ReverseGamepadStickLeanVertical = message.ToBoolean();
                });
            receiver.AssignCommandHandler(
                VmmCommands.SetDeviceTypeToStartWordToMotion,
                message => SetDeviceTypeForWordToMotion(message.ToInt())
                );
        }

        private void SetDeviceTypeForWordToMotion(int deviceType)
        {
            gamePadBasedBodyLean.UseGamepadForWordToMotion = (deviceType == DeviceTypeGamepad);
            handIkIntegrator.UseGamepadForWordToMotion = (deviceType == DeviceTypeGamepad);
            handIkIntegrator.UseKeyboardForWordToMotion = (deviceType == DeviceTypeKeyboardTenKey);
            handIkIntegrator.UseMidiControllerForWordToMotion = (deviceType == DeviceTypeMidiController);
        }

        //以下については適用先が1つじゃないことに注意

        private void SetLengthFromWristToTip(float v)
        {
            handIkIntegrator.Presentation.HandToTipLength = v;
            handIkIntegrator.Typing.HandToTipLength = v;
            handIkIntegrator.MouseMove.HandToTipLength = v;
            handIkIntegrator.MidiHand.WristToTipLength = v;
        }
        
        private void SetHandYOffsetBasic(float offset)
        {
            handIkIntegrator.Typing.YOffsetAlways = offset;
            handIkIntegrator.MouseMove.YOffset = offset;
            handIkIntegrator.MidiHand.HandOffsetAlways = offset;
        }

        private void SetHandYOffsetAfterKeyDown(float offset)
        {
            handIkIntegrator.Typing.YOffsetAfterKeyDown = offset;
            handIkIntegrator.MidiHand.HandOffsetAfterKeyDown = offset;
        }
    }
}
