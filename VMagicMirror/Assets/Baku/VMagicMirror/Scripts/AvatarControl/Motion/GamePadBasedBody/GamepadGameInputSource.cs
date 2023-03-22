using System;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.GameInput
{
    public enum GamepadMoveStickType
    {
        None,
        LeftStick,
        RightStick,
        LeftArrowButton,
    }

    //NOTE: めんどくさいからトリガーを一旦省いてるが、別に入れてもいい
    public enum GamepadLocomotionButtonAssign
    {
        None,
        A,
        B,
        X,
        Y,
    }

    public class GamepadGameInputSource : PresenterBase, IGameInputSource, ITickable
    {
        private const float MoveInputDiffPerSecond = 2f;
        private const float MoveInputDeadZone = 0.15f;

        bool IGameInputSource.IsActive => _isActive;
        IObservable<Vector2> IGameInputSource.MoveInput => _moveInput;
        IObservable<bool> IGameInputSource.IsCrouching => _isCrouching;
        public IObservable<Unit> Jump => _jump;

        private readonly XInputGamePad _gamepad;

        private bool _isActive;
        private Vector2 _rawMoveInput;
        private Vector2 _dumpedMoveInput;
        
        private readonly ReactiveProperty<Vector2> _moveInput = new ReactiveProperty<Vector2>();
        private readonly ReactiveProperty<bool> _isCrouching = new ReactiveProperty<bool>();
        private readonly Subject<Unit> _jump = new Subject<Unit>();

        //NOTE: スティックやジャンプ/しゃがみ指定の初期値はテキトーで、無いほうがマシなら未割り当てにしてもよい
        private readonly ReactiveProperty<GamepadMoveStickType> _stickType =
            new ReactiveProperty<GamepadMoveStickType>(GamepadMoveStickType.LeftStick);

        public GamepadKey JumpButton { get; set; } = GamepadKey.A;
        public GamepadKey CrouchButton { get; set; } = GamepadKey.X;

        private readonly IMessageReceiver _receiver;
        private CompositeDisposable _disposable;

        //TODO: 設定も受け取らないといけない
        public GamepadGameInputSource(
            IMessageReceiver receiver,
            XInputGamePad gamePad)
        {
            _receiver = receiver;
            _gamepad = gamePad;
        }

        public override void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.SetGamepadGameInputKeyAssign,
                command => ApplyGamepadKeyAssign(command.Content)
                );
        }

        void ITickable.Tick()
        {
            var diff = _rawMoveInput - _dumpedMoveInput;
            _dumpedMoveInput += Vector2.ClampMagnitude(diff, Mathf.Min(
                diff.magnitude,
                MoveInputDiffPerSecond * Time.deltaTime
            ));
            _moveInput.Value = GetMoveInputWithDeadZone(_dumpedMoveInput);
        }
        
        private void ApplyGamepadKeyAssign(string content)
        {
            
        }
        
        /// <summary>
        /// trueで呼び出すとゲームパッドの入力監視を開始する。
        /// falseで呼び出すと入力監視を終了する。必要ないうちは切っておくのを想定している
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
                return;
            }

            _disposable = new CompositeDisposable();
            _gamepad.ButtonUpDown
                .Subscribe(OnButtonUpDown)
                .AddTo(_disposable);

            _stickType.Select(s => s switch
                {
                    GamepadMoveStickType.LeftStick => _gamepad.LeftStickPosition,
                    GamepadMoveStickType.RightStick => _gamepad.RightStickPosition,
                    GamepadMoveStickType.LeftArrowButton => _gamepad
                        .ObserveEveryValueChanged(g => g.ArrowButtonsStickPosition),
                    _ => Observable.Return(Vector2Int.zero),
                })
                .Switch()
                .Subscribe(OnMoveInputChanged)
                .AddTo(_disposable);
        }
        
        private void OnButtonUpDown(GamepadKeyData data)
        {
            if (data.Key == JumpButton && data.IsPressed)
            {
                _jump.OnNext(Unit.Default);
            }

            if (data.Key == CrouchButton)
            {
                _isCrouching.Value = data.IsPressed;
            }
        }

        private void OnMoveInputChanged(Vector2Int moveInput)
        {
            Debug.Log("OnMoveINputChanged");
            var input = new Vector2(moveInput.x * 1f / 32768f, moveInput.y * 1f / 32768f);

            //NOTE: 更に「カメラに向かって奥方向かどうか」みたいなフラグも配慮したい気がする
            _rawMoveInput = input;
        }

        
        
        private static Vector2 GetMoveInputWithDeadZone(Vector2 input)
        {
            var magnitude = input.magnitude;
            // - 0 ~ DeadZone: ゼロ
            // - DeadZone ~ 1: 方向そのまま、大きさ0~1のベクトルに変形して割当
            if (magnitude < MoveInputDeadZone)
            {
                return Vector2.zero;
            }
            else
            {
                return input / magnitude * Mathf.InverseLerp(MoveInputDeadZone, 1f, magnitude);
            }
        }
    }
}
