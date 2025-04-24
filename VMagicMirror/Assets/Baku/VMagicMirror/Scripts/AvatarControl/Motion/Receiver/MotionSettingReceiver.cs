﻿using Baku.VMagicMirror.IK;
using UnityEngine;
using Zenject;
using UniRx;

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
        public void Initialize(
            IMessageReceiver receiver, 
            XInputGamePad gamePad,
            BodyMotionModeController motionModeController
            )
        {
            _gamePad = gamePad;

            motionModeController.MotionMode
                .Subscribe(mode =>
                {
                    var defaultMotionDisabled = mode != BodyMotionMode.Default;
                    handIkIntegrator.AlwaysHandDown.Value = defaultMotionDisabled;
                    gamePadBasedBodyLean.AlwaysHandDown = defaultMotionDisabled;
                });
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
                VmmCommands.PresentationArmRadiusMin,
                message =>
                    handIkIntegrator.Presentation.PresentationArmRadiusMin = message.ParseAsCentimeter()
                );
            receiver.AssignCommandHandler(
                VmmCommands.LookAtStyle,
                message => headIkIntegrator.SetLookAtStyle(message.GetStringValue())
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
                    gamePadBasedBodyLean.SetGamepadLeanMode(message.GetStringValue());
                    GamepadHandIk.SetGamepadLeanMode(message.GetStringValue());
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
            
            receiver.AssignCommandHandler(
                VmmCommands.SetGamepadMotionMode,
                v =>
                {
                    gamePadBasedBodyLean.SetGamepadMotionMode(v.ToInt());
                    handIkIntegrator.SetGamepadMotionMode(v.ToInt());
                });

            receiver.AssignCommandHandler(
                VmmCommands.SetKeyboardAndMouseMotionMode,
                v => handIkIntegrator.SetKeyboardAndMouseMotionMode(v.ToInt())
                ); 
        }

        private void SetDeviceTypeForWordToMotion(int deviceType)
        {
            gamePadBasedBodyLean.UseGamepadForWordToMotion = (deviceType == DeviceTypeGamepad);

            handIkIntegrator.WordToMotionDevice.Value = 
                (deviceType == DeviceTypeKeyboardWord) ? WordToMotionDeviceAssign.KeyboardWord :
                (deviceType == DeviceTypeGamepad) ? WordToMotionDeviceAssign.Gamepad :
                (deviceType == DeviceTypeKeyboardTenKey) ? WordToMotionDeviceAssign.KeyboardNumber :
                (deviceType == DeviceTypeMidiController) ? WordToMotionDeviceAssign.MidiController :
                WordToMotionDeviceAssign.None;
        }

        //以下については適用先が1つじゃないことに注意

        private void SetLengthFromWristToTip(float v)
        {
            handIkIntegrator.Presentation.HandToTipLength = v;
            handIkIntegrator.Typing.HandToTipLength = v;
            handIkIntegrator.MouseMove.HandToTipLength = v;
            handIkIntegrator.MidiHand.WristToTipLength = v;
            handIkIntegrator.CarHandle.WristToTipLength = v;
        }
        
        private void SetHandYOffsetBasic(float offset)
        {
            handIkIntegrator.YOffsetAlways = offset;
        }

        private void SetHandYOffsetAfterKeyDown(float offset)
        {
            handIkIntegrator.Typing.YOffsetAfterKeyDown = offset;
            handIkIntegrator.MidiHand.HandOffsetAfterKeyDown = offset;
        }
    }

    public enum KeyboardAndMouseMotionModes
    {
        /// <summary> 無反応 </summary>
        None = -1,
        /// <summary> デフォルトのキーボード+タッチパッド </summary>
        KeyboardAndTouchPad = 0,
        /// <summary> 右手はプレゼン指差し + 左手でキーボード </summary>
        Presentation = 1,
        /// <summary> ペンタブ + 左手は左手デバイスっぽい何か </summary>
        PenTablet = 2,
        /// <summary> 不明なため未サポート </summary>
        Unknown,
    }
}
