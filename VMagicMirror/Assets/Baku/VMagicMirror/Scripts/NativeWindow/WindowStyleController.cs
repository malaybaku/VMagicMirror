using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;
using Zenject;

namespace Baku.VMagicMirror
{
    using static NativeMethods;

    public class WindowStyleController : MonoBehaviour
    {
        static class TransparencyLevel
        {
            public const int None = 0;
            public const int WhenDragDisabledAndOnCharacter = 1;
            public const int WhenOnCharacter = 2;
            public const int WhenDragDisabled = 3;
            public const int Always = 4;
        }

        //Player Settingで決められるデフォルトウィンドウサイズと合わせてるが、常識的な値であれば多少ズレても害はないです
        private const int DefaultWindowWidth = 800;
        private const int DefaultWindowHeight = 600;

        [SerializeField] private float opaqueThreshold = 0.1f;
        [SerializeField] private float windowPositionCheckInterval = 5.0f;

        private float _windowPositionCheckCount;

        private uint _defaultWindowStyle;
        private uint _defaultExWindowStyle;

        private bool _isTransparent;
        private bool _isWindowFrameHidden;
        private bool _windowDraggableWhenFrameHidden = true;
        private bool _preferIgnoreMouseInput;

        private int _hitTestJudgeCountDown;
        //private bool _isMouseLeftButtonDownPreviousFrame = false;
        private bool _isDragging;
        private Vector2Int _dragStartMouseOffset = Vector2Int.zero;

        private bool _prevMousePositionInitialized;
        private Vector2 _prevMousePosition = Vector2.zero;

        private Renderer[] _renderers = Array.Empty<Renderer>();

        private Texture2D _colorPickerTexture;
        // NOTE: このフラグはアバターのバウンディングボックス内の不透明ピクセルにのみ反応し、
        // アクセサリーやサブキャラの不透明部分に対して反応することは保証されない
        private bool _isOnAvatarBoundingBoxOpaquePixel;
        // NOTE: こっちのフラグはcameraのRaycastベースで判定できるようなものに対する判定
        private bool _isOnNonAvatarOpaqueArea;
        private bool IsOnOpaquePixel => _isOnAvatarBoundingBoxOpaquePixel || _isOnNonAvatarOpaqueArea;
        
        private bool _isClickThrough;
        //既定値がtrueになる(デフォルトでは常時最前面である)ことに注意
        private bool _isTopMost = true;

        private int _wholeWindowTransparencyLevel = TransparencyLevel.WhenOnCharacter;
        private byte _wholeWindowAlphaWhenTransparent = 0x80;
        //ふつうに起動したら不透明ウィンドウ
        private byte _currentWindowAlpha = 0xFF;
        private const float AlphaLerpFactor = 0.2f;

        private CameraUtilWrapper _camera;
        private CameraBackgroundColorController _cameraBackgroundColorController;
        private WindowCropController _windowCropController;
        private Buddy.BuddyObjectRaycastChecker _buddyObjectRaycastChecker;
        private IDisposable _mouseObserve;

        private readonly WindowAreaIo _windowAreaIo = new();

        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            IMessageReceiver receiver, 
            IKeyMouseEventSource keyboardEventSource,
            CameraUtilWrapper cameraUtilWrapper,
            CameraBackgroundColorController cameraBackgroundColorController,
            WindowCropController windowCropController,
            Buddy.BuddyObjectRaycastChecker buddyObjectRaycastChecker
            )
        {
            _camera = cameraUtilWrapper;
            _buddyObjectRaycastChecker = buddyObjectRaycastChecker;
            _cameraBackgroundColorController = cameraBackgroundColorController;
            _windowCropController = windowCropController;

            receiver.AssignCommandHandler(
                VmmCommands.Chromakey,
                message =>
                {
                    var argb = message.ToColorFloats();
                    SetWindowTransparency(argb[0] == 0);
                });
            receiver.AssignCommandHandler(
                VmmCommands.WindowFrameVisibility,
                message => SetWindowFrameVisibility(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.IgnoreMouse,
                message => SetIgnoreMouseInput(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.TopMost,
                message => SetTopMost(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.WindowDraggable,
                message => SetWindowDraggable(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.MoveWindow,
                message =>
                {
                    var xy = message.ToIntArray();
                    MoveWindow(xy[0], xy[1]);
                });
            receiver.AssignCommandHandler(
                VmmCommands.ResetWindowSize,
                _ => ResetWindowSize()
                );
            receiver.AssignCommandHandler(
                VmmCommands.SetWholeWindowTransparencyLevel,
                message => SetTransparencyLevel(message.ToInt())
                );
            receiver.AssignCommandHandler(
                VmmCommands.SetAlphaValueOnTransparent,
                message => SetAlphaOnTransparent(message.ToInt())
                );

            _mouseObserve = keyboardEventSource.MouseButton.Subscribe(info =>
            {
                if (info == "LDown")
                {
                    ReserveHitTestJudgeOnNextFrame();
                }
            });
            
            vrmLoadable.PreVrmLoaded += info => _renderers = info.renderers;
            vrmLoadable.VrmDisposing += () => _renderers = Array.Empty<Renderer>();
        }

        private void Awake()
        {
            var hWnd = GetUnityWindowHandle();
            if (!Application.isEditor)
            {
                _defaultWindowStyle = GetWindowLong(hWnd, GWL_STYLE);
                _defaultExWindowStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
                //半透明許可のため、デフォルトで常時レイヤードウィンドウにしておく
                _defaultExWindowStyle |= WS_EX_LAYERED;
                SetWindowLong(hWnd, GWL_EXSTYLE, _defaultExWindowStyle);
            }

            CheckSettingFileDirect();
            _windowAreaIo.LoadAsync(this.GetCancellationTokenOnDestroy()).Forget();
            _windowPositionCheckCount = windowPositionCheckInterval;            
        }

        private void Start()
        {
            _colorPickerTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);

            SetTopMost(_isTopMost);
            Screen.fullScreen = false;
            StartCoroutine(PickColorCoroutine());
        }

        private void Update()
        {
            UpdatePointerOnNonAvatarOpaqueArea();
            UpdateClickThrough();
            UpdateDragStatus();
            UpdateWindowPositionCheck();
            UpdateWindowTransparency();
        }
        
        private void OnDestroy()
        {
            _windowAreaIo.Save();
            _mouseObserve?.Dispose();
            _mouseObserve = null;
        }

        private void CheckSettingFileDirect()
        {
            var reader = new DirectSettingFileReader();
            reader.Load();
            if (reader.TransparentBackground)
            {
                //NOTE: このif文の中身には、WPF側で「背景を透過」にチェックを入れた時の挙動の一部を入れているが、
                //見た目に関するものだけにしている(全部やるとクリックスルー設定が絡んで難しくなるので)
                SetWindowTransparency(true);
                SetWindowFrameVisibility(false);
                _cameraBackgroundColorController.ForceSetBackgroundTransparent();
            }
        }

        private void UpdatePointerOnNonAvatarOpaqueArea()
        {
            if (!_isTransparent)
            {
                // 不透明ウィンドウの場合、フラグが使われないので判定をサボっておく
                _isOnNonAvatarOpaqueArea = false;
                return;
            }

            _isOnNonAvatarOpaqueArea = _buddyObjectRaycastChecker.IsPointerOnBuddyObject();
        }

        private void UpdateClickThrough()
        {
            if (!_isTransparent)
            {
                //不透明ウィンドウ = 絶対にクリックは取る(不透明なのに裏ウィンドウが触れると不自然！)
                SetClickThrough(false);
                return;
            }

            if (!_windowDraggableWhenFrameHidden && _preferIgnoreMouseInput)
            {
                //透明であり、明示的にクリック無視が指定されている = 指定通りにクリックを無視
                SetClickThrough(true);
                return;
            }

            //透明であり、クリックはとってほしい = マウス直下のピクセル状態で判断
            SetClickThrough(!IsOnOpaquePixel);
        }

        private void UpdateDragStatus()
        {
            if (_isWindowFrameHidden &&
                _windowDraggableWhenFrameHidden &&
                _hitTestJudgeCountDown == 1 &&
                IsOnOpaquePixel
                )
            {
                _hitTestJudgeCountDown = 0;
                if (!Application.isFocused)
                {
                    SetUnityWindowActive();
                }
                _isDragging = true;

                if (!Application.isEditor)
                {
                    var mousePosition = GetWindowsMousePosition();
                    var windowPosition = GetUnityWindowPosition();
                    //以降、このオフセットを保てるようにウィンドウを動かす
                    _dragStartMouseOffset = mousePosition - windowPosition;
                }
            }

            //タッチスクリーンでパッと見の操作が破綻しないために…。
            if (!Input.GetMouseButton(0))
            {
                _isDragging = false;
            }

            if (_isDragging)
            {
                if (!Application.isEditor)
                {
                    var mousePosition = GetWindowsMousePosition();
                    SetUnityWindowPosition(
                        mousePosition.x - _dragStartMouseOffset.x, 
                        mousePosition.y - _dragStartMouseOffset.y
                    );
                }
            }

            if (_hitTestJudgeCountDown > 0)
            {
                _hitTestJudgeCountDown--;
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
            
            _windowAreaIo.Check();
        }

        private void UpdateWindowTransparency()
        {
            byte alpha = 0xFF;
            switch (_wholeWindowTransparencyLevel)
            {
                case TransparencyLevel.None:
                    //何もしない: 0xFFで不透明になればOK
                    break;
                case TransparencyLevel.WhenDragDisabledAndOnCharacter:
                    //NOTE: ここではalphaが変わるときサブキャラにインタラクトできないので、
                    //ポインターがメインアバターとサブキャラどちらにポインターが当たっても半透明にし、「操作できなさそう」感を出す
                    if (!_windowDraggableWhenFrameHidden && IsOnOpaquePixel)
                    {
                        alpha = _wholeWindowAlphaWhenTransparent;
                    }
                    break;
                case TransparencyLevel.WhenOnCharacter:
                    //NOTE: この条件ではIsOnOpaquePixelは使わず、サブキャラやセリフにポインターが当たったときは不透明のままにする。
                    // これにより、サブキャラやセリフUIへマウスオーバーしたときの「操作できそう」感を残す
                    if (_isOnAvatarBoundingBoxOpaquePixel)
                    {
                        alpha = _wholeWindowAlphaWhenTransparent;
                    }
                    break;
                case TransparencyLevel.WhenDragDisabled:
                    if (!_windowDraggableWhenFrameHidden)
                    {
                        alpha = _wholeWindowAlphaWhenTransparent;
                    }
                    break;
                case TransparencyLevel.Always:
                    alpha = _wholeWindowAlphaWhenTransparent;
                    break;
            }

            //ウィンドウが矩形なままになっているときは透けさせない
            //(ウィンドウが矩形なときはクリックスルーも認めてないので、透けさせないほうが一貫性が出る)
            if (!_isWindowFrameHidden)
            {
                alpha = 0xFF;
            }

            SetAlpha(alpha);
        }


        private void MoveWindow(int x, int y)
        {
            if (!Application.isEditor)
            {
                SetUnityWindowPosition(x, y);
            }
        }

        private void ResetWindowSize()
        {
            if (!Application.isEditor)
            {
                SetUnityWindowSize(DefaultWindowWidth, DefaultWindowHeight);
            }
        }

        private void SetWindowFrameVisibility(bool isVisible)
        {
            _isWindowFrameHidden = !isVisible;
            var hwnd = GetUnityWindowHandle();
            uint windowStyle = isVisible ? _defaultWindowStyle : WS_POPUP | WS_VISIBLE;
            if (!Application.isEditor)
            {
                SetWindowLong(hwnd, GWL_STYLE, windowStyle);
                SetUnityWindowTopMost(_isTopMost && _isWindowFrameHidden);
            }
        }

        private void SetWindowTransparency(bool isTransparent)
        {
            _isTransparent = isTransparent;
            if (!Application.isEditor)
            {
                SetDwmTransparent(isTransparent);
                ForceWindowResizeEvent();
            }
        }
        
        private void ForceWindowResizeEvent()
        {
            if (!GetWindowRect(GetUnityWindowHandle(), out var rect))
            {
                return;
            }

            //明示的に同じウィンドウサイズで再初期化することで、画像が歪むのを防ぐ
            int cx = rect.right - rect.left;
            int cy = rect.bottom - rect.top;
            RefreshWindowSize(cx, cy);
        }
        
        private void SetIgnoreMouseInput(bool ignoreMouseInput)
        {
            _preferIgnoreMouseInput = ignoreMouseInput;
        }

        private void SetTopMost(bool isTopMost)
        {
            _isTopMost = isTopMost;
            //NOTE: 背景透過をしてない = 普通のウィンドウが出てる間は別にTopMostじゃなくていいのがポイント
            if (!Application.isEditor)
            {
                SetUnityWindowTopMost(isTopMost && _isWindowFrameHidden);
            }
        }

        private void SetWindowDraggable(bool isDraggable)
        {
            if (!isDraggable)
            {
                _isDragging = false;
            }

            _windowDraggableWhenFrameHidden = isDraggable;
        }

        private void SetAlphaOnTransparent(int alpha)
        {
            _wholeWindowAlphaWhenTransparent = (byte)alpha;
        }

        private void SetTransparencyLevel(int level)
        {
            _wholeWindowTransparencyLevel = level;
        }

        private void ReserveHitTestJudgeOnNextFrame()
        {
            //UniRxのほうが実行が先なはずなので、
            //1カウント分はメッセージが来た時点のフレームで消費し、
            //さらに1フレーム待ってからヒットテスト判定
            _hitTestJudgeCountDown = 2;

            //タッチスクリーンだとMouseButtonUpが取れない事があるらしいため、この段階でフラグを折る
             _isDragging = false;
        }

        /// <summary>
        /// 背景を透過すべきかの判別方法: キルロボさんのブログ https://qiita.com/kirurobo/items/013cee3fa47a5332e186
        /// </summary>
        /// <returns></returns>
        private IEnumerator PickColorCoroutine()
        {
            var wait = new WaitForEndOfFrame();
            while (Application.isPlaying)
            {
                yield return wait;
                ObservePixelUnderCursor();
            }
            yield return null;
        }

        /// <summary> マウス直下の画素が透明かどうかを判定 </summary>
        private void ObservePixelUnderCursor()
        {
            if (!_prevMousePositionInitialized)
            {
                _prevMousePositionInitialized = true;
                _prevMousePosition = Input.mousePosition;
            }

            Vector2 mousePos = Input.mousePosition;
            //mouse does not move => not need to udpate opacity information
            if ((mousePos - _prevMousePosition).sqrMagnitude < Mathf.Epsilon)
            {
                return;
            }
            _prevMousePosition = mousePos;

            // マウスがウィンドウ外にある: 明らかに無視してよい
            if (!_camera.PixelRectContains(mousePos))
            {
                _isOnAvatarBoundingBoxOpaquePixel = false;
                return;
            }

            // 切り抜き処理をしている場合: 切り抜き図形の形状が決まってるのを使って判定
            if (_windowCropController.EnableCircleCrop.CurrentValue)
            {
                _isOnAvatarBoundingBoxOpaquePixel = _windowCropController.IsPointInsideCropArea(mousePos);
                return;
            }
            
            // アバターのAABB上にマウスが載ってない: 無視してよい
            if (!_windowCropController.EnableCircleCrop.CurrentValue &&
                !CheckMouseMightBeOnCharacter(mousePos))
            {
                _isOnAvatarBoundingBoxOpaquePixel = false;
                return;
            }

            try
            {
                // マウス直下の画素を読む (参考 http://tsubakit1.hateblo.jp/entry/20131203/1386000440 )
                _colorPickerTexture.ReadPixels(new Rect(mousePos, Vector2.one), 0, 0);
                var color = _colorPickerTexture.GetPixel(0, 0);

                // アルファ値がしきい値以上ならば不透過
                _isOnAvatarBoundingBoxOpaquePixel = (color.a >= opaqueThreshold);
            }
            catch (Exception ex)
            {
                if (Application.isEditor)
                {
                    // 稀に範囲外になるとのこと(元ブログに記載あり)
                    Debug.LogError(ex.Message);
                }
                else
                {
                    // runtimeではexは無視し、単に透明ピクセルだった事にしておく
                    _isOnAvatarBoundingBoxOpaquePixel = false;
                }
            }
        }

        private bool CheckMouseMightBeOnCharacter(Vector2 mousePosition)
        {
            var ray = _camera.ScreenPointToRay(mousePosition);
            //個別のメッシュでバウンディングボックスを調べることで大まかな判定になる仕組み。
            foreach (var r in _renderers)
            {
                if (r.bounds.IntersectRay(ray))
                {
                    return true;
                }
            }
            return false;
        }

        private void SetClickThrough(bool through)
        {
            if (_isClickThrough == through)
            {
                return;
            }

            _isClickThrough = through;

            var hwnd = GetUnityWindowHandle();
            uint exWindowStyle = _isClickThrough ?
                WS_EX_LAYERED | WS_EX_TRANSPARENT :
                _defaultExWindowStyle;

            if (!Application.isEditor)
            {
                SetWindowLong(hwnd, GWL_EXSTYLE, exWindowStyle);
            }
        }

        private void SetAlpha(byte alpha)
        {
            if (alpha == _currentWindowAlpha)
            {
                return;
            }

            byte newAlpha = (byte)Mathf.Clamp(
                Mathf.Lerp(_currentWindowAlpha, alpha, AlphaLerpFactor), 0, 255
                );
            //バイト値刻みで値が変わらないときは、最低でも1ずつ目標値のほうにずらしていく
            if (newAlpha == _currentWindowAlpha &&
                newAlpha != alpha)
            {
                newAlpha += (byte)Mathf.Sign(alpha - newAlpha);
            }

            _currentWindowAlpha = newAlpha;
            SetWindowAlpha(newAlpha);
        }
    }

    /// <summary>
    /// ウィンドウエリアの処理のうち、PlayerPrefsへのセーブ/ロードを含むような所だけ切り出したやつ
    /// </summary>
   class WindowAreaIo
    {
        //前回ソフトが終了したときのウィンドウの位置、およびサイズ
        private const string InitialPositionXKey = "InitialPositionX";
        private const string InitialPositionYKey = "InitialPositionY";
        private const string InitialWidthKey = "InitialWidth";
        private const string InitialHeightKey = "InitialHeight";

        private Vector2Int _prevWindowPosition = Vector2Int.zero;
        
        /// <summary>
        /// アプリ起動時に呼び出すことで、前回に保存した設定があればそれを読みこんで適用します。
        /// また、適用結果によってウィンドウがユーザーから見えない位置に移動した場合は復帰処理を行います。
        /// </summary>
        public async UniTaskVoid LoadAsync(CancellationToken cancellationToken)
        {
            if (PlayerPrefs.HasKey(InitialPositionXKey) && PlayerPrefs.HasKey(InitialPositionYKey))
            {
                var x = PlayerPrefs.GetInt(InitialPositionXKey);
                var y = PlayerPrefs.GetInt(InitialPositionYKey);
                if (!Application.isEditor)
                {
                    _prevWindowPosition = new Vector2Int(x, y);
                    SetUnityWindowPosition(x, y);
                }
            }
            else
            {
                if (!Application.isEditor)
                {
                    _prevWindowPosition = GetUnityWindowPosition();
                    PlayerPrefs.SetInt(InitialPositionXKey, _prevWindowPosition.x);
                    PlayerPrefs.SetInt(InitialPositionYKey, _prevWindowPosition.y);
                }
            }

            //前回起動時と同じモニター内にウィンドウを配置し終わってからサイズを適用するために待つ。
            //こうしないと、モニター間のDPI差がある環境で再起動のたびにウィンドウサイズがずれてしまう
            await UniTask.DelayFrame(6, cancellationToken: cancellationToken);
            
            var width = PlayerPrefs.GetInt(InitialWidthKey, 0);
            var height = PlayerPrefs.GetInt(InitialHeightKey, 0);
            if (width > 100 && height > 100)
            {
                if (!Application.isEditor)
                {
                    SetUnityWindowSize(width, height);
                }
            }

            AdjustIfWindowPositionInvalid();
        }
        
        /// <summary> アプリ起動中に呼び出すことで、ウィンドウが移動していればその位置を記録します。 </summary>
        public void Check() 
        {
            if (!Application.isEditor)
            {
                var pos = GetUnityWindowPosition();
                if (pos.x != _prevWindowPosition.x ||
                    pos.y != _prevWindowPosition.y)
                {
                    _prevWindowPosition = pos;
                    PlayerPrefs.SetInt(InitialPositionXKey, _prevWindowPosition.x);
                    PlayerPrefs.SetInt(InitialPositionYKey, _prevWindowPosition.y);
                }
            }
        }
        
        /// <summary> アプリ終了時に呼び出すことで、終了時のウィンドウ位置、およびサイズを記録します。 </summary>
        public void Save()
        {
            if (GetWindowRect(GetUnityWindowHandle(), out var rect))
            {
                if (!Application.isEditor)
                {
                    PlayerPrefs.SetInt(InitialPositionXKey, rect.left);
                    PlayerPrefs.SetInt(InitialPositionYKey, rect.top);
                    PlayerPrefs.SetInt(InitialWidthKey, rect.right - rect.left);
                    PlayerPrefs.SetInt(InitialHeightKey, rect.bottom - rect.top);
                }
            }
        }

        private void AdjustIfWindowPositionInvalid()
        {
            if (!GetWindowRect(GetUnityWindowHandle(), out var rect))
            {
                LogOutput.Instance.Write("Failed to get self window rect, could not start window position adjust");
                return;
            }
                
            var monitorRects = LoadAllMonitorRects();
            if (!CheckWindowPositionValidity(rect, monitorRects))
            {
                MoveToPrimaryMonitorBottomRight(rect);
            }         
        }

        private bool CheckWindowPositionValidity(RECT selfRect, List<RECT> monitorRects)
        {
            //ウィンドウ位置を正常とみなす条件: ウィンドウの「(左上 or 右上) + 中央上 + 中央」が、どれかのモニターの内側に含まれる
            var leftTop = new Vector2Int(selfRect.left, selfRect.top);
            var rightTop = new Vector2Int(selfRect.right, selfRect.top);
            var centerTop = new Vector2Int((selfRect.left + selfRect.right) / 2, selfRect.top);
            var center = new Vector2Int(
                (selfRect.left + selfRect.right) / 2,
                (selfRect.top + selfRect.bottom) / 2
            );

            return
                (IsInsideSomeRect(leftTop, monitorRects) || IsInsideSomeRect(rightTop, monitorRects)) &&
                IsInsideSomeRect(centerTop, monitorRects) &&
                IsInsideSomeRect(center, monitorRects);
        }

        private static bool IsInsideSomeRect(Vector2Int pos, List<RECT> rects)
        {
            return rects.Any(r => 
                pos.x >= r.left && pos.x < r.right && 
                pos.y >= r.top && pos.y < r.bottom
                );
        }
        
        //プライマリモニターの右下にウィンドウを移動したのち、ウィンドウサイズが大きすぎない事を保証し、
        //その状態でウィンドウの位置/サイズを保存します。
        //この処理は異常復帰なので、ちょっと余裕をもってウィンドウを動かすことに注意して下さい。
        private void MoveToPrimaryMonitorBottomRight(RECT selfRect)
        {
            var primaryWindowRect = GetPrimaryWindowRect();

            //ウィンドウを画面の4隅から確実に離せるようにサイズの上限 + 位置の調整を行う。
            //画面の4隅どこにタスクバーがあってもなるべく避けるとか、
            //透過ウィンドウを非透過にしたらタイトルバーが画面外に行っちゃった、というケースを避けるのが狙い
            int width = Mathf.Min(
                selfRect.right - selfRect.left,
                primaryWindowRect.right - primaryWindowRect.left - 200
                );
            int height = Mathf.Min(
                selfRect.bottom - selfRect.top,
                primaryWindowRect.bottom - primaryWindowRect.top - 200
                );
            
            int x = Mathf.Clamp(
                selfRect.left, primaryWindowRect.left + 100, primaryWindowRect.right - 100 - width
                );
            int y = Mathf.Clamp(
                selfRect.top, primaryWindowRect.top + 100, primaryWindowRect.bottom - 100 - height
                );

            if (!Application.isEditor)
            {
                _prevWindowPosition = new Vector2Int(x, y);
                PlayerPrefs.SetInt(InitialPositionXKey, x);
                PlayerPrefs.SetInt(InitialPositionYKey, y);
                PlayerPrefs.SetInt(InitialWidthKey, width);
                PlayerPrefs.SetInt(InitialHeightKey, height);

                SetUnityWindowPosition(x, y);
                SetUnityWindowSize(width, height);
            }
        }
    }
}