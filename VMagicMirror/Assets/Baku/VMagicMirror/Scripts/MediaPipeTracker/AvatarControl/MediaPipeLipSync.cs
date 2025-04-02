using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    using Keys = MediaPipeBlendShapeKeys;

    public class MediaPipeLipSync : ITickable
    {
        private readonly MediaPipeFacialValueRepository _facialValueRepository;
        private readonly MediapipePoseSetterSettings _settings;
        private readonly MediaPipeTrackerRuntimeSettingsRepository _runtimeSettings;

        private readonly RecordLipSyncSource _source = new();
        public IMouthLipSyncSource LipSyncSource => _source;

        [Inject]
        public MediaPipeLipSync(
            MediaPipeFacialValueRepository facialValueRepository,
            MediapipePoseSetterSettings settings,
            MediaPipeTrackerRuntimeSettingsRepository runtimeSettings)
        {
            _facialValueRepository = facialValueRepository;
            _settings = settings;
            _runtimeSettings = runtimeSettings;
        }
        
        private float BlendShapeMin => _settings.LipSyncBlendShapeMin;
        private float BlendShapeMax => _settings.LipSyncBlendShapeMax;
        public bool IsEnabledAndTracked => _runtimeSettings.ShouldUseLipSyncResult.Value && _facialValueRepository.IsTracked;

        void ITickable.Tick()
        {
            if (!_facialValueRepository.IsTracked)
            {
                _source.A = 0;
                _source.I = 0;
                _source.U = 0;
                _source.E = 0;
                _source.O = 0;
                return;
            }

            var jawOpen = _facialValueRepository.GetValue(Keys.jawOpen);
            var jawForward = _facialValueRepository.GetValue(Keys.jawForward);
            var mouthLeftSmile = _facialValueRepository.GetValue(Keys.mouthSmileLeft);
            var mouthRightSmile = _facialValueRepository.GetValue(Keys.mouthSmileRight);
            var mouthPucker = _facialValueRepository.GetValue(Keys.mouthPucker);
            var mouthFunnel = _facialValueRepository.GetValue(Keys.mouthFunnel);

            // NOTE: ExternalTrackerLipSync でも同じ補正をしているが、
            // ARKitとMediaPipeの特性が揃ってる必然性はあんま無いので、数値等を変えてもよい
            var a = MapClamp(2.0f * jawOpen);
            var i = MapClamp(0.6f * (mouthLeftSmile + mouthRightSmile) - 0.1f);
            var u = MapClamp(0.4f * (mouthPucker + mouthFunnel + jawForward));

            if (a + i + u > 1.0f)
            {
                var factor = 1.0f / (a + i + u);
                a *= factor;
                i *= factor;
                u *= factor;
            }

            _source.A = a;
            _source.I = i;
            _source.U = u;
        }
        
        //0-1の範囲の値をmin-maxの幅のなかにギュッとあれします
        private float MapClamp(float value)
            => Mathf.Clamp01((value - BlendShapeMin) / (BlendShapeMax - BlendShapeMin));
    }
}
