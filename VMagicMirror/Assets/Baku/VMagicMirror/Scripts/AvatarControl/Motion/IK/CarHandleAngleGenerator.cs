using System;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.IK
{
    //TODO: Deviceかどっかに移動してもよい (このクラス自体はあまりIK的ではない)
    /// <summary>
    /// ゲームパッド入力と現在のIK状態を踏まえてハンドルの回転状態を生成するクラス。
    /// このクラスが出力する角度が手とか姿勢の計算時のリファレンスになる
    /// </summary>
    public class CarHandleAngleGenerator : PresenterBase, ITickable
    {
        public const float MaxAngle = 540f;
        private const float StickValueToRateFactor = 1f / 32768f;

        //この秒数だけ入力がないとハンドル角度を中央に戻していく
        private const float CoolDownTime = 2.5f;
        //小さいスティック入力は無視する
        private const float StickThreshold = 0.3f;
        //2.5秒くらいでハンドルを回し切る
        private const float HandleRateChangeSpeed = 0.4f;
        //Dampをさっと入る
        private const float HandleRateChangeDampDuration = 0.4f;

        private readonly IMessageReceiver _receiver;
        private readonly BodyMotionModeController _bodyMotionModeController;
        private readonly HandIKIntegrator _handIKIntegrator;
        private readonly XInputGamePad _gamepad;

        private GamepadLeanModes _leanMode = GamepadLeanModes.GamepadLeanLeftStick;
        private bool _useCarHandle;
        private float _coolDownCount;

        private float _handleRateSpeed;
        private float _currentHandleRateTarget;

        private float _stickAxisX = 0f;
        private bool _invertStickValue;

        [Inject]
        public CarHandleAngleGenerator(
            IMessageReceiver receiver,
            BodyMotionModeController bodyMotionModeController,
            HandIKIntegrator handIKIntegrator, 
            XInputGamePad gamepad)
        {
            _receiver = receiver;
            _bodyMotionModeController = bodyMotionModeController;
            _handIKIntegrator = handIKIntegrator;
            _gamepad = gamepad;
        }

        private readonly ReactiveProperty<float> _handleRate = new(0f);
        /// <summary>
        /// -1 ~ +1 の範囲を取る値。角度は<see cref="HandleAngle"/>で取得できる
        /// </summary>
        public IReadOnlyReactiveProperty<float> HandleRate => _handleRate;

        public float HandleAngle => _handleRate.Value * MaxAngle;

        public override void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.GamepadLeanMode,
                command =>
                {
                    var leanModeName = command.Content;
                    _leanMode = Enum.TryParse<GamepadLeanModes>(leanModeName, out var result)
                        ? result
                        : GamepadLeanModes.GamepadLeanNone;
                });
            _receiver.AssignCommandHandler(
                VmmCommands.GamepadLeanReverseHorizontal,
                message => _invertStickValue = message.ToBoolean()
                );

            _handIKIntegrator.RightTargetType
                .CombineLatest(
                    _handIKIntegrator.LeftTargetType,
                    (right, left) =>
                        right is HandTargetType.CarHandle ||
                        left is HandTargetType.CarHandle
                )
                .Subscribe(usingCarHandle => _useCarHandle = usingCarHandle)
                .AddTo(this);

            _gamepad.LeftStickPosition
                .Where(_ => _leanMode is GamepadLeanModes.GamepadLeanLeftStick)
                .Subscribe(stick => _stickAxisX = stick.x * StickValueToRateFactor)
                .AddTo(this);

            _gamepad.RightStickPosition
                .Where(_ => _leanMode is GamepadLeanModes.GamepadLeanRightStick)
                .Subscribe(stick => _stickAxisX = stick.x * StickValueToRateFactor)
                .AddTo(this);
        }
        
        //NOTE: この計算自体は大して重くないので常に回しとく
        void ITickable.Tick()
        {
            if (_useCarHandle && _leanMode is GamepadLeanModes.GamepadLeanLeftButtons)
            {
                _stickAxisX = _gamepad.ArrowButtonsStickPosition.x * StickValueToRateFactor;
            }

            var hasInput =
                _bodyMotionModeController.MotionMode.Value is BodyMotionMode.Default &&
                _useCarHandle && 
                Mathf.Abs(_stickAxisX) > StickThreshold;

            var axis = 0f;
            if (hasInput)
            {
                //NOTE: 左右反転のポリシーの関係で「invertのとき符号反転しない」という書き方になっている。ちょっと変だけど意図的です
                axis = _invertStickValue ? _stickAxisX : -_stickAxisX;
            }

            if (hasInput)
            {
                _coolDownCount = 0f;
            }
            else if (!hasInput && _coolDownCount < CoolDownTime)
            {
                _coolDownCount += Time.deltaTime;
            }

            if (hasInput)
            {
                var diff = GetTargetRate(axis);
                _currentHandleRateTarget = Mathf.Clamp(
                    _currentHandleRateTarget + diff * HandleRateChangeSpeed * Time.deltaTime,
                    -1f,
                    1f
                );
            }
            else if (_coolDownCount >= CoolDownTime)
            {
                _currentHandleRateTarget = Mathf.MoveTowards(
                    _currentHandleRateTarget, 
                    0, 
                    0.4f * HandleRateChangeSpeed * Time.deltaTime
                );
            }

            _handleRate.Value = Mathf.SmoothDamp(
                _handleRate.Value,
                _currentHandleRateTarget,
                ref _handleRateSpeed,
                HandleRateChangeDampDuration,
                100f
            );
        }

        private float GetTargetRate(float stickAxis)
        {
            var sign = Mathf.Sign(stickAxis);
            var value = Mathf.InverseLerp(StickThreshold, 1f, Mathf.Abs(stickAxis));
            return sign * value;
        }
        
        /// <summary> どの入力をBodyLeanの値に反映するか考慮するやつ </summary>
        private enum GamepadLeanModes
        {
            GamepadLeanNone,
            GamepadLeanLeftButtons,
            GamepadLeanLeftStick,
            GamepadLeanRightStick,
        }
    }
}
