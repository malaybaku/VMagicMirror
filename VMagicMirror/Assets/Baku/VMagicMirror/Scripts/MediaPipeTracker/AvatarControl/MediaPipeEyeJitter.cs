using UnityEngine;
using Zenject;
using Keys = Baku.VMagicMirror.ExternalTracker.ExternalTrackerPerfectSync.Keys;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    /// <summary>
    /// MediaPipeによる表情トラッキングに基づいた眼球運動をするやつ。
    /// </summary>
    /// <remarks>
    /// <see cref="ExternalTrackerEyeJitter"/>に似ている…というか、コピーして実装している。
    /// ExTrackerと同じく、<see cref="EyeJitter"/>と排他的に動くのでJitterというクラス名になってるが、どちらかというとEyeMotionに近い
    /// </remarks>
    public class MediaPipeEyeJitter : ITickable, IEyeRotationRequestSource
    {
        //NOTE: Jitterと言ってるが値としてはユーザーの眼球運動そのものなので、大きめの運動として取り扱う
        private const float HorizontalShapeToRate = 1f;

        //上方向をあんまり適用すると白目が増えて怖いので…
        private const float VerticalUpShapeToRate = 0.5f;
        private const float VerticalDownShapeToRate = 1f;
        
        //基本的に制限を超えることはないが、一応やっておく
        private const float TotalBoneRotationRateLimit = 1.2f;

        private readonly MediaPipeFacialValueRepository _facialValueRepository;
        private readonly MediaPipeTrackerRuntimeSettingsRepository _runtimeSettings;

        [Inject]
        public MediaPipeEyeJitter(
            MediaPipeFacialValueRepository facialValueRepository,
            MediaPipeTrackerRuntimeSettingsRepository runtimeSettings
            )
        {
            _facialValueRepository = facialValueRepository;
            _runtimeSettings = runtimeSettings;
        }

        public bool IsEnabledAndTracked => _runtimeSettings.ShouldUseEyeResult && _facialValueRepository.IsTracked;

        public bool IsActive { get; set; }
        public Vector2 LeftEyeRotationRate { get; private set; }
        public Vector2 RightEyeRotationRate { get; private set; }

        void ITickable.Tick()
        {
            //NOTE: ここでガードしないようにして状態によらずスムージングする、みたいな実装もありうる
            if (!IsActive)
            {
                return;
            }
            
            var leftX = 
                _facialValueRepository.GetValue(Keys.EyeLookInLeft) - 
                _facialValueRepository.GetValue(Keys.EyeLookOutLeft);
            var leftY =
                _facialValueRepository.GetValue(Keys.EyeLookUpLeft) - 
                _facialValueRepository.GetValue(Keys.EyeLookDownLeft);
        
            var rightX = 
                _facialValueRepository.GetValue(Keys.EyeLookOutRight) -
                _facialValueRepository.GetValue(Keys.EyeLookInRight);
            var rightY = 
                _facialValueRepository.GetValue(Keys.EyeLookUpRight) -
                _facialValueRepository.GetValue(Keys.EyeLookDownRight);

            if (_runtimeSettings.IsFaceMirrored.Value)
            {
                (leftX, rightX) = (-rightX, -leftX);
                (leftY, rightY) = (rightY, leftY);
            }

            //NOTE: ClampMagnitudeがあるとかえって不自然かもしれないので外しても良いかも。
            LeftEyeRotationRate = GetRotationRate(
                leftX * HorizontalShapeToRate, 
                leftY > 0 ? leftY * VerticalUpShapeToRate : leftY * VerticalDownShapeToRate
            );
            RightEyeRotationRate = GetRotationRate(
                rightX * HorizontalShapeToRate, 
                rightY > 0 ? rightY * VerticalUpShapeToRate : rightY * VerticalDownShapeToRate
            );
        }

        private static Vector2 GetRotationRate(float x, float y)
        {
            return Vector2.ClampMagnitude(new Vector2(x, y), TotalBoneRotationRateLimit);
        }
    }
}
