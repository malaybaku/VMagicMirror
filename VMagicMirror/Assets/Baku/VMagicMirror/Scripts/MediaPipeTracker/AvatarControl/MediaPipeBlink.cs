using UnityEngine;
using Zenject;
using Keys = Baku.VMagicMirror.ExternalTracker.ExternalTrackerPerfectSync.Keys;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    public class MediaPipeBlink : ITickable
    {
        private readonly MediaPipeFacialValueRepository _facialSetter;
        private readonly MediaPipeTrackerRuntimeSettingsRepository _settingsRepository;
        private readonly MediapipePoseSetterSettings _poseSetterSettings;
        
        private readonly RecordBlinkSource _blinkSource = new();
        public IBlinkSource BlinkSource => _blinkSource;

        [Inject]
        public MediaPipeBlink(
            MediaPipeFacialValueRepository facialSetter,
            MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
            MediapipePoseSetterSettings poseSetterSettings)
        {
            _facialSetter = facialSetter;
            _settingsRepository = settingsRepository;
            _poseSetterSettings = poseSetterSettings;
        }

        public bool IsEnabledAndTracked => _settingsRepository.ShouldUseEyeResult && _facialSetter.IsTracked;
        
        void ITickable.Tick()
        {
            var subLimit = _poseSetterSettings.BlinkOpenSpeedMax * Time.deltaTime * 60f;

            var rightBlink = _facialSetter.GetValue(Keys.EyeBlinkRight);
            var rightSquint = _facialSetter.GetValue(Keys.EyeSquintRight);
            var leftBlink = _facialSetter.GetValue(Keys.EyeBlinkLeft);
            var leftSquint = _facialSetter.GetValue(Keys.EyeSquintLeft);
            var mirrored = _settingsRepository.IsFaceMirrored.Value;

            var rawLeftBlink = mirrored ? rightBlink : leftBlink;
            var rawLeftSquint = mirrored ? rightSquint : leftSquint;
            var rawRightBlink = mirrored ? leftBlink : rightBlink;
            var rawRightSquint = mirrored ? leftSquint : rightSquint;

            var left = MapClamp(rawLeftBlink);
            if (left < 0.9f)
            {
                left = Mathf.Lerp(left, _poseSetterSettings.BlinkValueOnSquint, rawLeftSquint);
            }
            //NOTE: 開くほうは速度制限があるけど閉じるほうは一瞬でいい、という方式。右目も同様。
            left = Mathf.Clamp(left, _blinkSource.Left - subLimit, 1.0f);
            _blinkSource.Left = Mathf.Clamp01(left);

            var right = MapClamp(rawRightBlink);
            if (right < 0.9f)
            {
                right = Mathf.Lerp(right, _poseSetterSettings.BlinkValueOnSquint, rawRightSquint);
            }
            right = Mathf.Clamp(right, _blinkSource.Right - subLimit, 1.0f);
            _blinkSource.Right = Mathf.Clamp01(right);
        }

        //0-1の範囲の値をmin-maxの幅のなかにギュッとあれします
        private float MapClamp(float value) => Mathf.Clamp01(
            (value - _poseSetterSettings.EyeMapMin) / (_poseSetterSettings.EyeMapMax - _poseSetterSettings.EyeMapMin)
            );
    }
}
