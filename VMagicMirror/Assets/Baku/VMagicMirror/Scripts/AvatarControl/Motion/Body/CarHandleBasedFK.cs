using Baku.VMagicMirror.IK;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 車のハンドル入力の状態に応じて各所のFK(体、頭、目)を計算するやつ。
    /// </summary>
    public class CarHandleBasedFK : IEyeRotationRequestSource
    {
        private const float BodyRotationAngleLimit = 3f;

        private readonly BodyMotionModeController _bodyMotionModeController;
        private readonly CarHandleProvider _carHandleProvider;
        private readonly CarHandleAngleGenerator _angleGenerator;

        [Inject]
        public CarHandleBasedFK(
            BodyMotionModeController bodyMotionModeController,
            CarHandleProvider carHandleProvider,
            CarHandleAngleGenerator angleGenerator)
        {
            _bodyMotionModeController = bodyMotionModeController;
            _carHandleProvider = carHandleProvider;
            _angleGenerator = angleGenerator;
        }

        //NOTE: 他にも何かありそうな気がするが一旦忘れとく
        public bool IsActive => 
            _bodyMotionModeController.MotionMode.CurrentValue is BodyMotionMode.Default &&
            _bodyMotionModeController.GamepadMotionMode.CurrentValue is GamepadMotionModes.CarController;

        public Vector2 LeftEyeRotationRate => GetEyeRotationRate();
        public Vector2 RightEyeRotationRate => GetEyeRotationRate();

        public Quaternion GetBodyLeanSuggest()
        {
            var handleRate = _angleGenerator.HandleRate.CurrentValue;
            var bodyRotationRate = Sigmoid(handleRate, 0.33f, 4);
            return Quaternion.Euler(0f, 0, bodyRotationRate * BodyRotationAngleLimit);
        }

        public Quaternion GetHeadYawRotation()
        {
            //NOTE: 左右反転の整合を取るために値をひっくり返す
            var rate = -_angleGenerator.HandleRate.CurrentValue;

            //NOTE: 0~90degあたりにほぼ不感になるエリアが欲しいのでカーブを使ってます
            var angleRate = Mathf.Sign(rate) * _carHandleProvider.GetHeadYawRateFromAngleRate(Mathf.Abs(rate));
            var angle = angleRate * 20f;
            return Quaternion.Euler(0f, angle, 0f);
        }

        private Vector2 GetEyeRotationRate()
        {
            //NOTE: 左右の整合性を取るために符号をひっくり返してます
            var rate = -_angleGenerator.HandleRate.CurrentValue;
            var rateX = Sigmoid(rate, .157f, 4);
            return new Vector2(rateX, 0f);
        }
        
        private static float Sigmoid(float value, float factor, float pow)
        {
            return 2f / (1 + Mathf.Pow(pow, -value / factor)) - 1f;
        }

    }
}
