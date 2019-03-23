using UnityEngine;

namespace Baku.VMagicMirror
{
    public class InputDeviceReceiver : MonoBehaviour
    {
        [SerializeField]
        InputDeviceToMotion _motion = null;

        private const float TouchPadVerticalOffset = 0.05f;

        private bool mousePositionInitialized = false;
        private int mousePositionX = 0;
        private int mousePositionY = 0;

        private Transform _keyboardRoot => _motion.keyboard.transform;
        private Transform _touchPadRoot => _motion.touchPad.transform.parent;

        public void ReceiveKeyPressed(string keyCodeName)
        {
            _motion?.PressKeyMotion(keyCodeName);
        }

        public void ReceiveMouseButton(string info)
        {
            if (info.Contains("Down"))
            {
                _motion?.ClickMotion(info);
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

        public void ReceiveMouseMove(int x, int y)
        {
            //WPFからマウスイベントをとる場合はこちらを使うが、今は無視
            //UpdateByXY(x, y);
        }

        private void UpdateByXY(int x, int y)
        {
            _motion.UpdateMouseBasedHeadTarget(x, y);

            if (
                mousePositionInitialized &&
                (mousePositionX != x || mousePositionY != y)
                )
            {
                _motion.GrabMouseMotion(x, y);
            }

            mousePositionX = x;
            mousePositionY = y;
            mousePositionInitialized = true;
        }

        public void SetLengthFromWristToPalm(float v)
        {
            _motion.handToPalmLength = v;
        }

        public void SetLengthFromWristToTip(float v)
        {
            _motion.handToTipLength = v;
        }

        public void EnableTouchTypingHeadMotion(bool v)
        {
            _motion.enableTouchTypingHeadMotion = v;
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
            _motion.keyboard.gameObject.SetActive(v);
            _motion.touchPad.gameObject.SetActive(v);
        }

        public void SetHandYOffsetBasic(float yoffset)
        {
            _motion.yOffsetAlways = yoffset;
        }

        public void SetHandYOffsetAfterKeyDown(float yoffset)
        {
            _motion.yOffsetAfterKeyDown = yoffset;
        }
    }
}
