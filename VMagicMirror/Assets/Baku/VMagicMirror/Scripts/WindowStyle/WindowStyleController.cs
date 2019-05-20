using System;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    using static NativeMethods;

    public class WindowStyleController : MonoBehaviour
    {
        //Player Settingで決められるデフォルトウィンドウサイズと合わせてるが、常識的な値であれば多少ズレても害はないです
        const int DefaultWindowWidth = 800;
        const int DefaultWindowHeight = 600;

        const string InitialPositionXKey = "InitialPositionX";
        const string InitialPositionYKey = "InitialPositionY";

        [SerializeField]
        private ReceivedMessageHandler handler = null;

        [SerializeField]
        private GrpcSender sender = null;

        [SerializeField]
        private float windowPositionCheckInterval = 5.0f;


        private float _windowPositionCheckCount = 0;
        private Vector2Int _prevWindowPosition = Vector2Int.zero;

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
                    case MessageCommandNames.ResetWindowSize:
                        ResetWindowSize();
                        break;
                    default:
                        break;
                }

            });

            //既定で最前面に表示
            SetTopMost(true);

            InitializeWindowPositionCheckStatus();
        }

        private void Update()
        {
            UpdateDragStatus();
            UpdateWindowPositionCheck();
        }

        private void OnDestroy()
        {
#if !UNITY_EDITOR
            var windowPosition = GetUnityWindowPosition();
            PlayerPrefs.SetInt(InitialPositionXKey, windowPosition.x);
            PlayerPrefs.SetInt(InitialPositionYKey, windowPosition.y);
#endif
        }

        private void UpdateDragStatus()
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

        private void InitializeWindowPositionCheckStatus()
        {
            _windowPositionCheckCount = windowPositionCheckInterval;
            if (PlayerPrefs.HasKey(InitialPositionXKey) &&
                PlayerPrefs.HasKey(InitialPositionYKey)
                )
            {
#if !UNITY_EDITOR
                int x = PlayerPrefs.GetInt(InitialPositionXKey);
                int y = PlayerPrefs.GetInt(InitialPositionYKey);
                _prevWindowPosition = new Vector2Int(x, y);
                SetUnityWindowPosition(x, y);
#endif
            }
            else
            {
#if !UNITY_EDITOR
                _prevWindowPosition = GetUnityWindowPosition();
                PlayerPrefs.SetInt(InitialPositionXKey, _prevWindowPosition.x);
                PlayerPrefs.SetInt(InitialPositionYKey, _prevWindowPosition.y);
#endif
            }
        }

        private void UpdateWindowPositionCheck()
        {
            _windowPositionCheckCount -= Time.deltaTime;
            if (_windowPositionCheckCount > 0)
            {
                return;
            }
            _windowPositionCheckCount = windowPositionCheckInterval;

#if !UNITY_EDITOR
            var pos = GetUnityWindowPosition();
            if (pos.x != _prevWindowPosition.x ||
                pos.y != _prevWindowPosition.y)
            {
                _prevWindowPosition = pos;
                PlayerPrefs.SetInt(InitialPositionXKey, _prevWindowPosition.x);
                PlayerPrefs.SetInt(InitialPositionYKey, _prevWindowPosition.y);
            }
#endif
        }

        private void MoveWindow(int x, int y)
        {
#if !UNITY_EDITOR
            SetUnityWindowPosition(x, y);
#endif
        }

        private void ResetWindowSize()
        {
#if !UNITY_EDITOR
            SetUnityWindowSize(DefaultWindowWidth, DefaultWindowHeight);
#endif
        }

        private void SetWindowFrameVisibility(bool isVisible)
        {
            _isWindowFrameHidden = !isVisible;

            LogOutput.Instance.Write($"{nameof(SetWindowFrameVisibility)}:{isVisible}");
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