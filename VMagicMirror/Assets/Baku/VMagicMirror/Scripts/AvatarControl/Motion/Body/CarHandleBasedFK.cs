using Baku.VMagicMirror.IK;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 車のハンドル入力の状態に応じて各所のFK(体、頭、目)を計算するやつ。
    /// </summary>
    public class CarHandleBasedFK
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
            _bodyMotionModeController.MotionMode.Value is BodyMotionMode.Default &&
            _bodyMotionModeController.GamepadMotionMode.Value is GamepadMotionModes.CarController;
        
        public Quaternion GetBodyLeanSuggest()
        {
            var handleRate = _angleGenerator.HandleRate.Value;
            var bodyRotationRate = Sigmoid(handleRate, 0.33f, 4);
            return Quaternion.Euler(0f, 0, bodyRotationRate * BodyRotationAngleLimit);
        }

        public Quaternion GetHeadYawRotation()
        {
            var rate = _angleGenerator.HandleRate.Value;
            //NOTE: 0~90degあたりにほぼ不感になるエリアが欲しいのでカーブを使ってます
            var angleRate = Mathf.Sign(rate) * _carHandleProvider.GetHeadYawRateFromAngleRate(rate);
            var angle = angleRate * 30f;
            return Quaternion.Euler(0f, angle, 0f);
        }

        public float GetEyeRotationRate()
        {
            var rate = _angleGenerator.HandleRate.Value;
            return Sigmoid(rate, .157f, 4);
        }
        
        private static float Sigmoid(float value, float factor, float pow)
        {
            return 2f / (1 + Mathf.Pow(pow, -value / factor)) - 1f;
        }

    }
}
