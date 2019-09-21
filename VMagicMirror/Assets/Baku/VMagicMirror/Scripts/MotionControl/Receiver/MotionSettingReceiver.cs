﻿using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary> モーション関係で、操作ではなく設定値を受け取るレシーバクラス </summary>
    public class MotionSettingReceiver : MonoBehaviour
    {
        [SerializeField] private ReceivedMessageHandler handler = null;

        [SerializeField] private GamepadBasedBodyLean gamePadBasedBodyLean = null;

        [SerializeField] private HandIKIntegrator handIkIntegrator = null;

        [SerializeField] private HeadIkIntegrator headIkIntegrator = null;

        [SerializeField] private IkWeightCrossFade ikWeightCrossFade = null;
        
        [SerializeField] private StatefulXinputGamePad gamePad = null;

        private void Start()
        {
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.EnableHidArmMotion:
                        handIkIntegrator.EnableHidArmMotion = message.ToBoolean();
                        ikWeightCrossFade.ForceStopHandIk = !message.ToBoolean();
                        break;
                    case MessageCommandNames.LengthFromWristToPalm:
                        SetLengthFromWristToPalm(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.LengthFromWristToTip:
                        handIkIntegrator.Typing.HandToTipLength = message.ParseAsCentimeter();
                        break;
                    case MessageCommandNames.HandYOffsetBasic:
                        SetHandYOffsetBasic(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.HandYOffsetAfterKeyDown:
                        SetHandYOffsetAfterKeyDown(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.EnablePresenterMotion:
                        handIkIntegrator.EnablePresentationMode = message.ToBoolean();
                        break;
                    case MessageCommandNames.PresentationArmMotionScale:
                        handIkIntegrator.Presentation.PresentationArmMotionScale = message.ParseAsPercentage();
                        break;
                    case MessageCommandNames.PresentationArmRadiusMin:
                        handIkIntegrator.Presentation.PresentationArmRadiusMin = message.ParseAsCentimeter();
                        break;
                    case MessageCommandNames.LookAtStyle:
                        headIkIntegrator.SetLookAtStyle(message.Content);
                        break;
                    case MessageCommandNames.EnableGamepad:
                        gamePad.enabled = message.ToBoolean();
                        break;
                    case MessageCommandNames.GamepadLeanMode:
                        gamePadBasedBodyLean.SetGamepadLeanMode(message.Content);
                        break;
                    case MessageCommandNames.GamepadLeanReverseHorizontal:
                        gamePadBasedBodyLean.ReverseGamepadStickLeanHorizontal = message.ToBoolean();
                        break;
                    case MessageCommandNames.GamepadLeanReverseVertical:
                        gamePadBasedBodyLean.ReverseGamepadStickLeanVertical = message.ToBoolean();
                        break;
                }

            });
        }
        
        //以下については適用先が1つじゃないことに注意
        
        private void SetLengthFromWristToPalm(float v)
        {
            handIkIntegrator.Gamepad.HandToPalmLength = v;
            handIkIntegrator.MouseMove.HandToPalmLength = v;
        }
        
        private void SetHandYOffsetBasic(float offset)
        {
            handIkIntegrator.Gamepad.YOffsetAlways = offset;
            handIkIntegrator.Typing.YOffsetAlways = offset;
            handIkIntegrator.MouseMove.YOffset = offset;
        }

        private void SetHandYOffsetAfterKeyDown(float offset)
        {
            handIkIntegrator.Gamepad.YOffsetAfterKeyDown = offset;
            handIkIntegrator.Typing.YOffsetAfterKeyDown = offset;
        }
    }
}
