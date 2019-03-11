using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror
{
    public class InputDeviceReceiver : MonoBehaviour
    {
        [SerializeField]
        InputDeviceToMotion _motion = null;

        [SerializeField]
        Text posX = null;

        [SerializeField]
        Text posY = null;

        [SerializeField]
        Text mouseButton = null;

        [SerializeField]
        Text keyCode = null;

        private const float TouchPadVerticalOffset = 0.05f;

        private bool mousePositionInitialized = false;
        private int mousePositionX = 0;
        private int mousePositionY = 0;

        private Transform _keyboardRoot => _motion.keyboard.transform;
        private Transform _touchPadRoot => _motion.touchPad.transform.parent;

        public void ReceiveKeyPressed(string keyCodeName)
        {
            if (keyCode!= null)
            {
                keyCode.text = keyCodeName;
            }

            _motion?.PressKeyMotion(keyCodeName);
        }

        public void ReceiveMouseButton(string info)
        {
            if (mouseButton != null)
            {
                mouseButton.text = info;
            }

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
            int x = (int)pos.x;
            int y = (int)pos.y;

            if (!mousePositionInitialized)
            {
                UpdateMousePostionIndication(x, y);
                mousePositionX = x;
                mousePositionY = y;
                mousePositionInitialized = true;
                return;
            }

            if (mousePositionX != x || mousePositionY != y)
            {
                UpdateMousePostionIndication(x, y);
                mousePositionX = x;
                mousePositionY = y;
                _motion?.GrabMouseMotion(x, y);
            }
        }

        private void UpdateMousePostionIndication(int x, int y)
        {
            if (posX != null)
            {
                posX.text = x.ToString();
            }
            if (posY != null)
            {
                posY.text = y.ToString();
            }
        }

        public void SetLengthFromWristToPalm(float v)
        {
            _motion.handToPalmLength = v;
        }

        public void SetLengthFromWristToTip(float v)
        {
            _motion.handToTipLength = v;
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
    }
}
