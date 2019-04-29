using UnityEngine;
using UniRx;
using System;

namespace Baku.VMagicMirror
{
    public class InputDeviceReceiver : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        [SerializeField]
        private InputDeviceToMotion motion = null;

        [SerializeField]
        private StatefulXinputGamePad gamePad = null;

        private bool mousePositionInitialized = false;
        private int mousePositionX = 0;
        private int mousePositionY = 0;

        private GamepadLeanModes _leanMode = GamepadLeanModes.GamepadLeanLeftStick;
        private bool _reverseGamepadStickLeanHorizontal = false;
        private bool _reverseGamepadStickLeanVertical = false;

        private void Start()
        {
            SubscribeMessageHandler();
            SubscribeGamepad();
        }

        private void Update()
        {
            //NOTE: Unityが非アクティブのときの値取得については Run in Backgroundを前提として
            // - マウス位置: OK, Input.mousePositionで取れる。
            // - マウスクリック: NG, グローバルフック必須
            // - キーボード: NG, グローバルフック必須
            var pos = Input.mousePosition;
            UpdateByXY((int)pos.x, (int)pos.y);
        }

        private void SubscribeMessageHandler()
        {
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.KeyDown:
                        ReceiveKeyPressed(message.Content);
                        break;
                    case MessageCommandNames.MouseButton:
                        ReceiveMouseButton(message.Content);
                        break;
                    case MessageCommandNames.MouseMoved:
                        int[] xy = message.ToIntArray();
                        ReceiveMouseMove(xy[0], xy[1]);
                        break;
                    case MessageCommandNames.LengthFromWristToPalm:
                        SetLengthFromWristToPalm(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.LengthFromWristToTip:
                        SetLengthFromWristToTip(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.HandYOffsetBasic:
                        SetHandYOffsetBasic(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.HandYOffsetAfterKeyDown:
                        SetHandYOffsetAfterKeyDown(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.EnablePresenterMotion:
                        EnablePresenterMotion(message.ToBoolean());
                        break;
                    case MessageCommandNames.PresentationArmMotionScale:
                        SetPresentationArmMotionScale(message.ParseAsPercentage());
                        break;
                    case MessageCommandNames.PresentationArmRadiusMin:
                        SetPresentationArmRadiusMin(message.ParseAsCentimeter());
                        break;
                    case MessageCommandNames.EnableTouchTyping:
                        EnableTouchTypingHeadMotion(message.ToBoolean());
                        break;
                    case MessageCommandNames.EnableGamepad:
                        EnableGamepad(message.ToBoolean());
                        break;
                    case MessageCommandNames.GamepadLeanMode:
                        SetGamepadLeanMode(message.Content);
                        break;
                    case MessageCommandNames.GamepadLeanReverseHorizontal:
                        SetGamepadLeanReverseHorizontal(message.ToBoolean());
                        break;
                    case MessageCommandNames.GamepadLeanReverseVertical:
                        SetGamepadLeanReverseVertical(message.ToBoolean());
                        break;
                    default:
                        break;
                }

            });
        }

        private void EnablePresenterMotion(bool v) => motion.EnablePresentationMotion = v;
        private void SetPresentationArmMotionScale(float v) => motion.presentationArmMotionScale = v;
        private void SetPresentationArmRadiusMin(float v) => motion.presentationArmRadiusMin = v;

        private void SubscribeGamepad()
        {
            gamePad.ButtonUpDown.Subscribe(data =>
            {
                if (data.IsPressed)
                {
                    motion.GamepadButtonDown(data.Key);
                    if (_leanMode == GamepadLeanModes.GamepadLeanLeftButtons)
                    {
                        ApplyLeanMotion(
                            NormalizedStickPos(gamePad.ArrowButtonsStickPosition)
                            );
                    }
                }
                else
                {
                    motion.GamepadButtonUp(data.Key);
                    if (_leanMode == GamepadLeanModes.GamepadLeanLeftButtons)
                    {
                        ApplyLeanMotion(
                            NormalizedStickPos(gamePad.ArrowButtonsStickPosition)
                            );
                    }
                }
            });

            gamePad.LeftStickPosition.Subscribe(pos =>
            {
                var stickPos = NormalizedStickPos(pos);
                motion.GamepadLeftStick(stickPos);

                if (_leanMode == GamepadLeanModes.GamepadLeanLeftStick)
                {
                    ApplyLeanMotion(stickPos);
                }
            });

            gamePad.RightStickPosition.Subscribe(pos =>
            {
                var stickPos = NormalizedStickPos(pos);
                motion.GamepadRightStick(stickPos);

                if (_leanMode == GamepadLeanModes.GamepadLeanRightStick)
                {
                    ApplyLeanMotion(stickPos);
                }
            });

       
        }

        private static Vector2 NormalizedStickPos(Vector2Int v)
        {
            const float Factor = 1.0f / 32768.0f;
            return new Vector2(v.x * Factor, v.y * Factor);
        }

        private void ApplyLeanMotion(Vector2 pos)
        {
            var reverseConsideredPos = new Vector2(
                pos.x * (_reverseGamepadStickLeanHorizontal ? -1f : 1f),
                pos.y * (_reverseGamepadStickLeanVertical ? -1f : 1f)
                );


            motion.GamepadLeanMotion(reverseConsideredPos);
        }

        private void UpdateByXY(int x, int y)
        {
            motion.UpdateMouseBasedHeadTarget(x, y);

            if (
                mousePositionInitialized &&
                (mousePositionX != x || mousePositionY != y)
                )
            {
                motion.GrabMouseMotion(x, y);
            }

            mousePositionX = x;
            mousePositionY = y;
            mousePositionInitialized = true;
        }

        private void ReceiveKeyPressed(string keyCodeName)
        {
            motion?.PressKeyMotion(keyCodeName);
        }

        private void ReceiveMouseButton(string info)
        {
            if (info.Contains("Down"))
            {
                motion?.ClickMotion(info);
            }
        }

        private void ReceiveMouseMove(int x, int y)
        {
            //WPFからマウスイベントをとる場合はこちらを使うが、今は無視
            //UpdateByXY(x, y);
        }

        private void SetLengthFromWristToPalm(float v)
        {
            motion.handToPalmLength = v;
        }

        private void SetLengthFromWristToTip(float v)
        {
            motion.handToTipLength = v;
        }

        private void EnableTouchTypingHeadMotion(bool v)
        {
            motion.enableTouchTypingHeadMotion = v;
        }

        private void SetHandYOffsetBasic(float yoffset)
        {
            motion.yOffsetAlways = yoffset;
        }

        private void SetHandYOffsetAfterKeyDown(float yoffset)
        {
            motion.yOffsetAfterKeyDown = yoffset;
        }


        private void SetGamepadLeanMode(string leanModeName)
        {
            _leanMode =
                Enum.TryParse<GamepadLeanModes>(leanModeName, out var result) ?
                result :
                GamepadLeanModes.GamepadLeanNone;

            if (_leanMode == GamepadLeanModes.GamepadLeanNone)
            {
                ApplyLeanMotion(Vector2.zero);
            }
        }

        private void SetGamepadLeanReverseHorizontal(bool reverse)
        {
            _reverseGamepadStickLeanHorizontal = reverse;
        }

        private void SetGamepadLeanReverseVertical(bool reverse)
        {
            _reverseGamepadStickLeanVertical = reverse;
        }

        private void EnableGamepad(bool isEnabled)
        {
            gamePad.enabled = isEnabled;
        }


        private enum GamepadLeanModes
        {
            GamepadLeanNone,
            GamepadLeanLeftButtons,
            GamepadLeanLeftStick,
            GamepadLeanRightStick,
        }
        
    }
}
