using System;
using System.Windows.Forms;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.GameInput
{
    public class KeyboardGameInputSource : PresenterBase, ITickable, IGameInputSource
    {
        //NOTE: DPIが96の場合の値
        private const float LookAroundNormalizeFactor = 200;
        private const float MouseMoveThrottleCount = 0.6f;

        #region Interface 
        
        IObservable<Vector2> IGameInputSource.MoveInput => _moveInput;
        IObservable<Vector2> IGameInputSource.LookAroundInput => _lookAroundInput;

        IObservable<bool> IGameInputSource.IsCrouching => _isCrouching;
        IObservable<bool> IGameInputSource.IsRunWalkToggleActive => _isRunning;
        IObservable<bool> IGameInputSource.GunFire => _gunFire;
        IObservable<Unit> IGameInputSource.Jump => _jump;
        IObservable<Unit> IGameInputSource.Punch => _punch;
        
        #endregion
        
        private bool _isActive;
        private readonly ReactiveProperty<Vector2> _moveInput = new ReactiveProperty<Vector2>();
        private readonly ReactiveProperty<Vector2> _lookAroundInput = new ReactiveProperty<Vector2>();
        private readonly ReactiveProperty<bool> _isCrouching = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<bool> _isRunning = new ReactiveProperty<bool>();
        private readonly ReactiveProperty<bool> _gunFire = new ReactiveProperty<bool>();
        private readonly Subject<Unit> _jump = new Subject<Unit>();
        private readonly Subject<Unit> _punch = new Subject<Unit>();

        private readonly IKeyMouseEventSource _keySource;
        private readonly IMessageReceiver _receiver;
        private readonly MousePositionProvider _mousePositionProvider;

        private CompositeDisposable _disposable;
        //NOTE: 個別のフラグ値がメッセージで飛んでくるものについては、このKeyAssignの中身よりもフラグ値が優先される
        private KeyboardGameInputKeyAssign _keyAssign = KeyboardGameInputKeyAssign.LoadDefault();
        
        //TODO: 本当はコッチのフラグは使わずKeyAssignに寄せたい
        private bool _useMouseLookAround = true;
        private bool _useWasdMove = true;
        private bool _useArrowKeyMove = true;
        private bool _useShiftRun = true;
        private bool _useSpaceJump = true;

        private bool _forwardKeyPressed;
        private bool _backKeyPressed;
        private bool _leftKeyPressed;
        private bool _rightKeyPressed;


        private bool _hasPrevRawPosition;
        private Vector2Int _prevRawPosition;
        
        //マウスが動き続けている間はその移動値が蓄積され、そうでなければ0になるような値
        private Vector2Int _rawDiffSum;
        //マウスが動き続けている間は0より大きくなる値
        private float _mouseMoveCountDown;

        public bool MouseMoveLookAroundActive => _isActive && _useMouseLookAround;
        
        public KeyboardGameInputSource(
            IKeyMouseEventSource keySource,
            MousePositionProvider mousePositionProvider,
            IMessageReceiver receiver)
        {
            _keySource = keySource;
            _receiver = receiver;
            _mousePositionProvider = mousePositionProvider;
        }

        public override void Initialize()
        {
            LogOutput.Instance.Write($"screen dpi = {Screen.dpi:0.00}");
            
            _receiver.AssignCommandHandler(
                VmmCommands.SetKeyboardGameInputKeyAssign,
                command => UpdateKeyAssign(command.Content)
            );
            
            _receiver.AssignCommandHandler(
                VmmCommands.EnableWasdMoveGameInput,
                command => _useWasdMove = command.ToBoolean()
            );
            _receiver.AssignCommandHandler(
                VmmCommands.EnableArrowKeyMoveGameInput,
                command => _useArrowKeyMove = command.ToBoolean()
            );
            _receiver.AssignCommandHandler(
                VmmCommands.UseShiftRunGameInput,
                command => _useShiftRun = command.ToBoolean()
            );
            _receiver.AssignCommandHandler(
                VmmCommands.UseSpaceJumpGameInput,
                command => _useSpaceJump = command.ToBoolean()
            );
            _receiver.AssignCommandHandler(
                VmmCommands.UseMouseMoveForLookAroundGameInput,
                command => _useMouseLookAround = command.ToBoolean()
            );
        }

        void ITickable.Tick()
        {
            if (!_isActive || !_useMouseLookAround)
            {
                _mouseMoveCountDown = 0f;
                _rawDiffSum = Vector2Int.zero;
                _lookAroundInput.Value = Vector2.zero;
                return;
            }

            //マウス移動の取り扱いのアプローチ
            // - マウスの位置ではなく移動値に頼る。FPSだとマウスロックかかる事もあるのでそれ前提で考える
            // - ちょっとだけmagnitudeも使うが、わりと首がガン振りされてもよい前提で考える
            // - 一定時間マウスが動かないことを「入力がゼロに戻った」事象とみなす
            var rawPos = _mousePositionProvider.RawPosition;
            var rawDiff = rawPos - _prevRawPosition;
            if (!_hasPrevRawPosition)
            {
                rawDiff = Vector2Int.zero;
                _hasPrevRawPosition = true;
            }
            _prevRawPosition = rawPos;

            if (rawDiff == Vector2Int.zero)
            {
                if (_mouseMoveCountDown > 0f)
                {
                    _mouseMoveCountDown -= Time.deltaTime;
                    if (_mouseMoveCountDown <= 0f)
                    {
                        _rawDiffSum = Vector2Int.zero;
                        _lookAroundInput.Value = Vector2.zero;
                    }
                }
            }
            else
            {
                //NOTE: この積算だと「右上 -> 左上」みたくマウス動かしたときに真上を指すが、それは想定挙動とする
                _rawDiffSum += new Vector2Int(rawDiff.x, rawDiff.y);
                var dpiRate = Screen.dpi / 96f;
                if (dpiRate <= 0f)
                {
                    dpiRate = 1f;
                }

                //rawDiffでは下方向が正であることに注意！
                var input = (dpiRate / LookAroundNormalizeFactor) * new Vector2(_rawDiffSum.x, -_rawDiffSum.y);
                _lookAroundInput.Value = Vector2.ClampMagnitude(input, 1f);
                _mouseMoveCountDown = MouseMoveThrottleCount;
            }
        }
        
        public void SetActive(bool active)
        {
            if (_isActive == active)
            {
                return;
            }

            _isActive = active;
            _disposable?.Dispose();
            if (!active)
            {
                ResetInputs();
                return;
            }

            _disposable = new CompositeDisposable();
            _keySource.RawKeyUp
                .Subscribe(OnKeyUp)
                .AddTo(_disposable);
            _keySource.RawKeyDown
                .Subscribe(OnKeyDown)
                .AddTo(_disposable);

            _keySource.MouseButton
                .Subscribe(OnMouseKeyUpDown)
                .AddTo(_disposable);
        }

        private void UpdateKeyAssign(string json)
        {
            try
            {
                var setting = JsonUtility.FromJson<KeyboardGameInputKeyAssign>(json);
                ApplyKeyAssign(setting);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
        
        private void ApplyKeyAssign(KeyboardGameInputKeyAssign setting)
        {
            _keyAssign = setting;
            if (_isActive)
            {
                SetActive(false);
                SetActive(true);
            }
        }

        private void ResetInputs()
        {
            _hasPrevRawPosition = false;
            _prevRawPosition = Vector2Int.zero;
            _rawDiffSum = Vector2Int.zero;
            _mouseMoveCountDown = 0f;

            _moveInput.Value = Vector2.zero;
            _lookAroundInput.Value = Vector2.zero;
            _isRunning.Value = false;
            _isCrouching.Value = false;
            _gunFire.Value = false;

            _forwardKeyPressed = false;
            _backKeyPressed = false;
            _leftKeyPressed = false;
            _rightKeyPressed = false;
        }

        private void OnKeyDown(string key)
        {
            if (_useShiftRun && 
                (key == nameof(Keys.ShiftKey) || key == nameof(Keys.LShiftKey) || key == nameof(Keys.RShiftKey))
                )
            {
                _isRunning.Value = true;
                //shiftはコレ以外の入力に使ってないはずなので無視
                return;
            }

            if (_useSpaceJump && key == nameof(Keys.Space))
            {
                //Spaceもこの用途でのみ使うはず
                _jump.OnNext(Unit.Default);
                return;
            }

            if (_useWasdMove)
            {
                if (key == nameof(Keys.W)) _forwardKeyPressed = true;
                if (key == nameof(Keys.A)) _leftKeyPressed = true;
                if (key == nameof(Keys.S)) _backKeyPressed = true;
                if (key == nameof(Keys.D)) _rightKeyPressed = true;
            }

            if (_useArrowKeyMove)
            {
                if (key == nameof(Keys.Up)) _forwardKeyPressed = true;
                if (key == nameof(Keys.Left)) _leftKeyPressed = true;
                if (key == nameof(Keys.Down)) _backKeyPressed = true;
                if (key == nameof(Keys.Right)) _rightKeyPressed = true;
            }

            //NOTE: MoveInputは変化しないことのほうが多いが、冗長に呼んでおく
            UpdateMoveInput();
        }

        private void OnKeyUp(string key)
        {
            if (_useShiftRun && 
                (key == nameof(Keys.ShiftKey) || key == nameof(Keys.LShiftKey) || key == nameof(Keys.RShiftKey))
               )
            {
                _isRunning.Value = false;
                return;
            }

            if (_useWasdMove)
            {
                if (key == "W") _forwardKeyPressed = false;
                if (key == "A") _leftKeyPressed = false;
                if (key == "S") _backKeyPressed = false;
                if (key == "D") _rightKeyPressed = false;
            }

            if (_useArrowKeyMove)
            {
                if (key == nameof(Keys.Up)) _forwardKeyPressed = false;
                if (key == nameof(Keys.Left)) _leftKeyPressed = false;
                if (key == nameof(Keys.Down)) _backKeyPressed = false;
                if (key == nameof(Keys.Right)) _rightKeyPressed = false;
            }

            //NOTE: MoveInputは変化しないことのほうが多いが、まあそれはそれとして。
            UpdateMoveInput();
        }

        private void OnMouseKeyUpDown(string button)
        {
            switch (button)
            {
                case MouseButtonEventNames.LDown:
                    ApplyMouseInput(_keyAssign.LeftClick, true);
                    return;
                case MouseButtonEventNames.LUp:
                    ApplyMouseInput(_keyAssign.LeftClick, false);
                    return;
                case MouseButtonEventNames.RDown:
                    ApplyMouseInput(_keyAssign.RightClick, true);
                    return;
                case MouseButtonEventNames.RUp:
                    ApplyMouseInput(_keyAssign.RightClick, false);
                    return;
                case MouseButtonEventNames.MDown:
                    ApplyMouseInput(_keyAssign.MiddleClick, true);
                    return;
                case MouseButtonEventNames.MUp:
                    ApplyMouseInput(_keyAssign.MiddleClick, false);
                    return;
            }
        }

        private void ApplyMouseInput(GameInputButtonAction action, bool pressed)
        {
            if (action == GameInputButtonAction.None)
            {
                return;
            }

            switch (action)
            {
                case GameInputButtonAction.Run:
                    _isRunning.Value = pressed;
                    return;
                case GameInputButtonAction.Crouch:
                    _isCrouching.Value = pressed;
                    return;
                case GameInputButtonAction.Trigger:
                    _gunFire.Value = pressed;
                    return;
            }

            if (!pressed)
            {
                return;
            }

            switch (action)
            {
                case GameInputButtonAction.Jump:
                    _jump.OnNext(Unit.Default);
                    return;
                case GameInputButtonAction.Punch:
                    _punch.OnNext(Unit.Default);
                    return;
            }
        }
        
        private void UpdateMoveInput()
        {
            //斜め入力時にmagnitude = Sqrt(2)となるのを禁止: ゲームパッドの入力と揃えるため
            _moveInput.Value = Vector2.ClampMagnitude(
                new Vector2(
                    (_rightKeyPressed ? 1f : 0f) + (_leftKeyPressed ? -1f : 0f),
                    (_forwardKeyPressed ? 1f : 0f) + (_backKeyPressed ? -1f : 0f)
                ), 
                1f
                );
        }

        public override void Dispose()
        {
            base.Dispose();
            _disposable?.Dispose();
            _disposable = null;
        }
    }
}
