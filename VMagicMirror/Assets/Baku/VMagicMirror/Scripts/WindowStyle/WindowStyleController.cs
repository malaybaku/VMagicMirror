using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    using static NativeMethods;

    public class WindowStyleController : MonoBehaviour
    {
        private uint defaultWindowStyle;
        private uint defaultExWindowStyle;

        private bool _isTransparent = false;
        private bool _isWindowFrameHidden = false;
        private bool _windowDraggableWhenFrameHidden = true;

        private bool _isDragging = false;
        private Vector2Int _dragStartMouseOffset = Vector2Int.zero;

        private void Awake()
        {
            defaultWindowStyle = GetWindowLong(GetUnityWindowHandle(), GWL_STYLE);
            defaultExWindowStyle = GetWindowLong(GetUnityWindowHandle(), GWL_EXSTYLE);

            _isTransparent = false;
            _isWindowFrameHidden = false;
            _windowDraggableWhenFrameHidden = true;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) && 
                _isWindowFrameHidden && 
                _windowDraggableWhenFrameHidden)
            {
                _isDragging = true;
#if !UNITY_EDITOR
                var mousePosition = GetWindowsMousePosition();
                var windowPosition = GetUnityWindowPosition();
                //以降、このオフセットを保てるようにウィンドウを動かす
                _dragStartMouseOffset = mousePosition - windowPosition;
#endif
            }

            if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }

            if (_isDragging)
            {
#if !UNITY_EDITOR
                var mousePosition = GetWindowsMousePosition();
                SetUnityWindowPosition(
                    mousePosition.x - _dragStartMouseOffset.x, 
                    mousePosition.y - _dragStartMouseOffset.y
                    );
#endif
            }
        }

        public void MoveWindow(int x, int y)
        {
#if !UNITY_EDITOR
            SetUnityWindowPosition(x, y);
#endif
        }

        public void SetWindowFrameVisibility(bool isVisible)
        {
            _isWindowFrameHidden = !isVisible;

            Debug.Log($"{nameof(SetWindowFrameVisibility)}:{isVisible}");
            var hwnd = GetUnityWindowHandle();
            uint windowStyle = isVisible ?
                defaultWindowStyle :
                WS_POPUP | WS_VISIBLE;
#if !UNITY_EDITOR
            SetWindowLong(hwnd, GWL_STYLE, windowStyle);
#endif
        }

        public void SetWindowTransparency(bool isTransparent)
        {
            _isTransparent = isTransparent;
            Debug.Log($"{nameof(SetWindowTransparency)}:{isTransparent}");
#if !UNITY_EDITOR
            SetDwmTransparent(isTransparent);
#endif
        }

        public void SetIgnoreMouseInput(bool ignoreMouseInput)
        {
            var hwnd = GetUnityWindowHandle();
            uint exWindowStyle = ignoreMouseInput ?
                WS_EX_LAYERED | WS_EX_TRANSPARENT :
                defaultExWindowStyle;

#if !UNITY_EDITOR
            SetWindowLong(hwnd, GWL_EXSTYLE, exWindowStyle);
#endif
        }

        public void SetTopMost(bool isTopMost)
        {
#if !UNITY_EDITOR
        SetUnityWindowTopMost(isTopMost);
#endif
        }

        public void SetWindowDraggable(bool isDraggable)
        {
            if (!isDraggable)
            {
                _isDragging = false;
            }

            _windowDraggableWhenFrameHidden = isDraggable;
        }

    }
}