using System;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.GameInput
{
    public class GamepadGameInputSource : PresenterBase, IGameInputSource
    {
        //コントローラによっては0付近で値がカチカチするので…
        private const float StickDeadZone = 0.15f;

        #region Interface
        
        IObservable<Vector2> IGameInputSource.MoveInput => _moveInput;
        IObservable<Vector2> IGameInputSource.LookAroundInput => _lookAroundInput;
        IObservable<bool> IGameInputSource.IsCrouching => _isCrouching;
        IObservable<bool> IGameInputSource.IsRunWalkToggleActive => _isRunning;
        IObservable<bool> IGameInputSource.GunFire => _gunFire;
        IObservable<Unit> IGameInputSource.Jump => _jump;
        IObservable<Unit> IGameInputSource.Punch => _punch;
        IObservable<string> IGameInputSource.StartCustomMotion => _customMotion;
        IObservable<string> IGameInputSource.StopCustomMotion => _stopCustomMotion;
        
        #endregion
        
        private readonly XInputGamePad _gamepad;
        private readonly IMessageReceiver _receiver;
        private CompositeDisposable _disposable;

        private readonly ReactiveProperty<Vector2> _moveInput = new();
        private readonly ReactiveProperty<Vector2> _lookAroundInput = new();
        private readonly ReactiveProperty<bool> _isCrouching = new();
        private readonly ReactiveProperty<bool> _isRunning = new();
        private readonly ReactiveProperty<bool> _gunFire = new();
        private readonly Subject<Unit> _jump = new();
        private readonly Subject<Unit> _punch = new();
        private readonly Subject<string> _customMotion = new();
        private readonly Subject<string> _stopCustomMotion = new();
        
        private bool _isActive;
        private GamepadGameInputKeyAssign _keyAssign = GamepadGameInputKeyAssign.LoadDefault();
        
        public GamepadGameInputSource(IMessageReceiver receiver, XInputGamePad gamePad)
        {
            _receiver = receiver;
            _gamepad = gamePad;
        }

        public override void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.SetGamepadGameInputKeyAssign,
                command => ApplyGamepadKeyAssign(command.StringValue)
                );
        }

        private void ApplyGamepadKeyAssign(string content)
        {
            try
            {
                var setting = JsonUtility.FromJson<GamepadGameInputKeyAssign>(content);
                ApplyKeyAssign(setting);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void ResetInput()
        {
            _moveInput.Value = Vector2.zero;
            _lookAroundInput.Value = Vector2.zero;

            _isRunning.Value = false;
            _isCrouching.Value = false;
            _gunFire.Value = false;
        }
        
        private void ApplyKeyAssign(GamepadGameInputKeyAssign setting)
        {
            _keyAssign = setting;
            if (_isActive)
            {
                SetActive(false);
                SetActive(true);
            }
        }

        /// <summary>
        /// trueで呼び出すとゲームパッドの入力監視を開始する。
        /// falseで呼び出すと入力監視を終了する。必要ないうちは切っておくのを想定している
        /// この処理はキーアサインが変わるときも呼ばれる
        /// </summary>
        /// <param name="active"></param>
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
                ResetInput();
                return;
            }

            _disposable = new CompositeDisposable();
            _gamepad.ButtonUpDown
                .Subscribe(OnButtonUpDown)
                .AddTo(_disposable);

            if (_keyAssign.StickLeft != GameInputStickAction.None)
            {
                _gamepad.LeftStickPosition
                    .Subscribe(v => OnStickUpdated(v, _keyAssign.StickLeft))
                    .AddTo(_disposable);
            }
            
            if (_keyAssign.StickRight != GameInputStickAction.None)
            {
                _gamepad.RightStickPosition
                    .Subscribe(v => OnStickUpdated(v, _keyAssign.StickRight))
                    .AddTo(_disposable);
            }

            if (_keyAssign.DPadLeft != GameInputStickAction.None)
            {
                _gamepad.ObserveEveryValueChanged(g => g.ArrowButtonsStickPosition)
                    .Subscribe(v => OnStickUpdated(v, _keyAssign.DPadLeft))
                    .AddTo(_disposable);
            }
        }
        
        private void OnButtonUpDown(GamepadKeyData data)
        {
            var action = GetButtonAction(data.Key);
            if (action == GameInputButtonAction.None)
            {
                return;
            }

            //NOTE: 複数ボタンに割当たっている入力は後勝ちさせる。ちょっと変だが安定はするので。
            if (action == GameInputButtonAction.Run)
            {
                _isRunning.Value = data.IsPressed;
                return;
            }
            
            if (action == GameInputButtonAction.Crouch)
            {
                _isCrouching.Value = data.IsPressed;
                return;
            }

            if (action == GameInputButtonAction.Trigger)
            {
                _gunFire.Value = data.IsPressed;
                return;
            }

            if (action is GameInputButtonAction.Custom)
            {
                var key = GetButtonActionCustomKey(data.Key);
                if (!string.IsNullOrEmpty(key))
                {
                    if (data.IsPressed)
                    {
                        _customMotion.OnNext(key);
                    }
                    else
                    {
                        _stopCustomMotion.OnNext(key);
                    }
                }
            }
            
            //Trigger系の挙動はボタン下げでのみ起こる
            if (!data.IsPressed)
            {
                return;
            }

            switch (action)
            {
                case GameInputButtonAction.Punch:
                    _punch.OnNext(Unit.Default);
                    break;
                case GameInputButtonAction.Jump:
                    _jump.OnNext(Unit.Default);
                    break;
            }
        }

        private void OnStickUpdated(Vector2Int value, GameInputStickAction action)
        {
            if (action == GameInputStickAction.None)
            {
                return;
            }
            
            var input = new Vector2(value.x * 1f / 32768f, value.y * 1f / 32768f);
            switch (action)
            {
                case GameInputStickAction.Move:
                    _moveInput.Value = GetInputWithDeadZone(input);
                    return;
                case GameInputStickAction.LookAround:
                    _lookAroundInput.Value = GetInputWithDeadZone(input);
                    return;
            }
        }

        private static Vector2 GetInputWithDeadZone(Vector2 input)
        {
            var magnitude = input.magnitude;
            // - 0 ~ DeadZone: ゼロ
            // - DeadZone ~ 1: 方向そのまま、大きさ0~1のベクトルに変形して割当
            if (magnitude < StickDeadZone)
            {
                return Vector2.zero;
            }
            else
            {
                return input / magnitude * Mathf.InverseLerp(StickDeadZone, 1f, magnitude);
            }
        }

        private GameInputButtonAction GetButtonAction(GamepadKey key)
        {
            return key switch
            {
                GamepadKey.A => _keyAssign.ButtonA,
                GamepadKey.B => _keyAssign.ButtonB,
                GamepadKey.X => _keyAssign.ButtonX,
                GamepadKey.Y => _keyAssign.ButtonY,
                GamepadKey.LShoulder => _keyAssign.ButtonLButton,
                GamepadKey.LTrigger => _keyAssign.ButtonLTrigger,
                GamepadKey.RShoulder => _keyAssign.ButtonRButton,
                GamepadKey.RTrigger=> _keyAssign.ButtonRTrigger,
                GamepadKey.Select => _keyAssign.ButtonView,
                GamepadKey.Start => _keyAssign.ButtonMenu,
                
                _ => GameInputButtonAction.None,
            };
        }

        private string GetButtonActionCustomKey(GamepadKey key)
        {
            return key switch
            {
                GamepadKey.A => _keyAssign.CustomButtonAKey,
                GamepadKey.B => _keyAssign.CustomButtonBKey,
                GamepadKey.X => _keyAssign.CustomButtonXKey,
                GamepadKey.Y => _keyAssign.CustomButtonYKey,
                GamepadKey.LShoulder => _keyAssign.CustomButtonLButtonKey,
                GamepadKey.LTrigger => _keyAssign.CustomButtonLTriggerKey,
                GamepadKey.RShoulder => _keyAssign.CustomButtonRButtonKey,
                GamepadKey.RTrigger=> _keyAssign.CustomButtonRTriggerKey,
                GamepadKey.Select => _keyAssign.CustomButtonViewKey,
                GamepadKey.Start => _keyAssign.CustomButtonMenuKey,
                _ => "",
            };
        }

        public override void Dispose()
        {
            base.Dispose();
            _disposable?.Dispose();
            _disposable = null;
        }
    }
}
