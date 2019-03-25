using UnityEngine;
using UniRx;

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

        private void SubscribeGamepad()
        {
            gamePad.ButtonUpDown.Subscribe(data =>
            {
                if (data.IsPressed)
                {
                    motion.GamepadButtonDown(data.Key);
                }
                else
                {
                    motion.GamepadButtonUp(data.Key);
                }
            });

            gamePad.LeftStickPosition.Subscribe(pos =>
            {
                //Debug.Log($"LStick: {pos.x}, {pos.y}");
                motion.GamepadLeftStick(FromShortVector2Int(pos));
            });

            gamePad.RightStickPosition.Subscribe(pos =>
            {
                //Debug.Log($"RStick: {pos.x}, {pos.y}");
                motion.GamepadRightStick(FromShortVector2Int(pos));
            });

            Vector2 FromShortVector2Int(Vector2Int v)
            {
                const float Factor = 1.0f / 32768.0f;
                return new Vector2(v.x * Factor, v.y * Factor);
            }
        }

        private void SubscribeMessageHandler()
        {
            handler.Messages.Subscribe(message =>
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
                    case MessageCommandNames.EnableTouchTyping:
                        EnableTouchTypingHeadMotion(message.ToBoolean());
                        break;
                    default:
                        break;
                }

            });
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
    }
}
