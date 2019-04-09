using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    using static NativeMethods;

    public class WindowStyleController : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        private uint defaultWindowStyle;
        private uint defaultExWindowStyle;

        private bool _isTransparent = false;
        private bool _isWindowFrameHidden = false;
        private bool _windowDraggableWhenFrameHidden = true;

        private bool _isDragging = false;
        private Vector2Int _dragStartMouseOffset = Vector2Int.zero;

        private void Awake()
        {
#if !UNITY_EDITOR
            defaultWindowStyle = GetWindowLong(GetUnityWindowHandle(), GWL_STYLE);
            defaultExWindowStyle = GetWindowLong(GetUnityWindowHandle(), GWL_EXSTYLE);
#endif
        }

        private void Start()
        {
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.Chromakey:
                        var argb = message.ToColorFloats();
                        SetWindowTransparency(argb[0] == 0);
                        break;
                    case MessageCommandNames.WindowFrameVisibility:
                        SetWindowFrameVisibility(message.ToBoolean());
                        break;
                    case MessageCommandNames.IgnoreMouse:
                        SetIgnoreMouseInput(message.ToBoolean());
                        break;
                    case MessageCommandNames.TopMost:
                        SetTopMost(message.ToBoolean());
                        break;
                    case MessageCommandNames.WindowDraggable:
                        SetWindowDraggable(message.ToBoolean());
                        break;
                    case MessageCommandNames.MoveWindow:
                        int[] xy = message.ToIntArray();
                        MoveWindow(xy[0], xy[1]);
                        break;
                    default:
                        break;
                }

            });
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

        private void MoveWindow(int x, int y)
        {
#if !UNITY_EDITOR
            SetUnityWindowPosition(x, y);
#endif
        }

        private void SetWindowFrameVisibility(bool isVisible)
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

        private void SetWindowTransparency(bool isTransparent)
        {
            _isTransparent = isTransparent;
#if !UNITY_EDITOR
            SetDwmTransparent(isTransparent);
#endif
        }

        private void SetIgnoreMouseInput(bool ignoreMouseInput)
        {
            var hwnd = GetUnityWindowHandle();
            uint exWindowStyle = ignoreMouseInput ?
                WS_EX_LAYERED | WS_EX_TRANSPARENT :
                defaultExWindowStyle;

#if !UNITY_EDITOR
            SetWindowLong(hwnd, GWL_EXSTYLE, exWindowStyle);
#endif
        }

        private void SetTopMost(bool isTopMost)
        {
#if !UNITY_EDITOR
        SetUnityWindowTopMost(isTopMost);
#endif
        }

        private void SetWindowDraggable(bool isDraggable)
        {
            if (!isDraggable)
            {
                _isDragging = false;
            }

            _windowDraggableWhenFrameHidden = isDraggable;
        }

    }
}