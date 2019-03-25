using System;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class InputDeviceReceiver : MonoBehaviour
    {
        [SerializeField]
        InputDeviceToMotion motion = null;

        [SerializeField]
        StatefulXinputGamePad gamePad = null;

        private const float TouchPadVerticalOffset = 0.05f;

        private bool mousePositionInitialized = false;
        private int mousePositionX = 0;
        private int mousePositionY = 0;

        private Transform _keyboardRoot => motion.keyboard.transform;
        private Transform _touchPadRoot => motion.touchPad.transform.parent;

        public void ReceiveKeyPressed(string keyCodeName)
        {
            motion?.PressKeyMotion(keyCodeName);
        }

        public void ReceiveMouseButton(string info)
        {
            if (info.Contains("Down"))
            {
                motion?.ClickMotion(info);
            }
        }

        public void ReceiveMouseMove(int x, int y)
        {
            //WPFからマウスイベントをとる場合はこちらを使うが、今は無視
            //UpdateByXY(x, y);
        }

        private void Start()
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

        private void Update()
        {
            //NOTE: Unityが非アクティブのときの値取得については Run in Backgroundを前提として
            // - マウス位置: OK, Input.mousePositionで取れる。
            // - マウスクリック: NG, グローバルフック必須
            // - キーボード: NG, グローバルフック必須
            var pos = Input.mousePosition;
            UpdateByXY((int)pos.x, (int)pos.y);
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

        public void SetLengthFromWristToPalm(float v)
        {
            motion.handToPalmLength = v;
        }

        public void SetLengthFromWristToTip(float v)
        {
            motion.handToTipLength = v;
        }

        public void EnableTouchTypingHeadMotion(bool v)
        {
            motion.enableTouchTypingHeadMotion = v;
        }

        public void SetHidHeight(float v)
        {
            var keyboardPos = _keyboardRoot.position;
            _keyboardRoot.position = new Vector3(keyboardPos.x, v, keyboardPos.z);

            var touchPadPos = _touchPadRoot.position;
            _touchPadRoot.position = new Vector3(touchPadPos.x, v + TouchPadVerticalOffset, touchPadPos.z);
        }

        public void SetHidHorizontalScale(float v)
        {
            _keyboardRoot.localScale = new Vector3(v, 1.0f, v);
            _touchPadRoot.localScale = new Vector3(v, 1.0f, v);
        }

        public void SetHidVisibility(bool v)
        {
            motion.keyboard.gameObject.SetActive(v);
            motion.touchPad.gameObject.SetActive(v);
        }

        public void SetHandYOffsetBasic(float yoffset)
        {
            motion.yOffsetAlways = yoffset;
        }

        public void SetHandYOffsetAfterKeyDown(float yoffset)
        {
            motion.yOffsetAfterKeyDown = yoffset;
        }
    }
}
