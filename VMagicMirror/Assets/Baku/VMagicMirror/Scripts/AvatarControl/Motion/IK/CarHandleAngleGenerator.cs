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

        //デフォルト角度からハンドルいっぱいに回すまでにかかる秒数
        private const float HandleMoveDuration = 2.2f;

        //小さいスティック入力は無視する
        private const float StickThreshold = 0.3f;

        private readonly IMessageReceiver _receiver;
        private readonly BodyMotionModeController _bodyMotionModeController;
        private readonly HandIKIntegrator _handIKIntegrator;
        private readonly XInputGamePad _gamepad;

        private GamepadLeanModes _leanMode;
        private bool _useCarHandle;
        private float _coolDownCount;

        private float _handleRateSpeed;

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

        private float _stickAxisX = 0f;
        private bool _invertStickValue;
        
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
                axis = _invertStickValue ? -_stickAxisX : _stickAxisX;
            }

            if (!hasInput && _coolDownCount < CoolDownTime)
            {
                _coolDownCount += Time.deltaTime;
            }

            var targetValue = 0f;
            if (hasInput)
            {
                targetValue = GetTargetRate(axis);
            }
            else if (_coolDownCount >= CoolDownTime)
            {
                targetValue = 0f;
            }
            else
            {
                //値が据え置きになるように
                targetValue = _handleRate.Value;
            }

            _handleRate.Value =  Mathf.SmoothDamp(
                _handleRate.Value,
                targetValue,
                ref _handleRateSpeed,
                HandleMoveDuration,
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
