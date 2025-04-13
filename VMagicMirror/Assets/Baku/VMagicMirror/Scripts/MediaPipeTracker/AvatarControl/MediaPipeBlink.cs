using UnityEngine;
using Zenject;

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
        
        void ITickable.Tick()
        {
            var subLimit = _poseSetterSettings.BlinkOpenSpeedMax * Time.deltaTime * 60f;

            var eye = _facialSetter.BlendShapes.Eye;
            var mirrored = _settingsRepository.IsFaceMirrored.Value;

            // NOTE: iFacialMocap連携の実装ではSquintのブレンドシェイプ値も参照しているが、
            // MediaPipeでは値があまり動かなくて信用できないので無視する (= 半目を安定してアバターに適用することは諦める)
            var rawLeftBlink = mirrored ? eye.RightBlink : eye.LeftBlink;
            var rawRightBlink = mirrored ? eye.LeftBlink : eye.RightBlink;

            //NOTE: 開くほうは速度制限があるけど閉じるほうは一瞬でいい、という方式。右目も同様。
            var left = MapClamp(rawLeftBlink);
            left = Mathf.Clamp(left, _blinkSource.Left - subLimit, 1.0f);
            _blinkSource.Left = Mathf.Clamp01(left);

            var right = MapClamp(rawRightBlink);
            right = Mathf.Clamp(right, _blinkSource.Right - subLimit, 1.0f);
            _blinkSource.Right = Mathf.Clamp01(right);
        }

        //0-1の範囲の値をmin-maxの幅のなかにギュッとあれします
        private float MapClamp(float value) => Mathf.Clamp01(
            (value - _settingsRepository.EyeOpenBlinkValue) / (_settingsRepository.EyeCloseBlinkValue - _settingsRepository.EyeOpenBlinkValue)
            );
    }
}
