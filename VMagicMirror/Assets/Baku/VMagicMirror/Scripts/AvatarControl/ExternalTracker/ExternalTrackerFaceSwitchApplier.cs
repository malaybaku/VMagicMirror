using System.Collections.Generic;
using UnityEngine;
using Zenject;
using VRM;
using Baku.VMagicMirror.ExternalTracker;

namespace Baku.VMagicMirror
{
    public class ExternalTrackerFaceSwitchApplier : MonoBehaviour
    {
        private bool _hasModel = false;
        private FaceControlConfiguration _config;
        private ExternalTrackerDataSource _externalTracker;
        private EyeBonePostProcess _eyeBoneResetter;

        //NOTE: 毎回Keyを生成するとGCAlloc警察に怒られるので、回数が減るように書いてます
        private string _latestClipName = "";
        private BlendShapeKey _latestKey = default;
        
        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            FaceControlConfiguration config,
            ExternalTrackerDataSource externalTracker,
            EyeBonePostProcess eyeBoneResetter
            )
        {
            _config = config;
            _externalTracker = externalTracker;
            _eyeBoneResetter = eyeBoneResetter;

            vrmLoadable.VrmLoaded += info =>
            {
                _hasModel = true;
            };
            
            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
            };
        }

        public bool HasClipToApply => !string.IsNullOrEmpty(_externalTracker.FaceSwitchClipName);
        public bool KeepLipSync => _externalTracker.KeepLipSyncForFaceSwitch;

        public void Accumulate(VRMBlendShapeProxy proxy)
        {
            if (!_hasModel ||
                string.IsNullOrEmpty(_externalTracker.FaceSwitchClipName) || 
                _config.WordToMotionExpressionActive
            )
            {
                return;
            }
            
            if (_latestClipName != _externalTracker.FaceSwitchClipName)
            {
                _latestKey = CreateKey(_externalTracker.FaceSwitchClipName);
                _latestClipName = _externalTracker.FaceSwitchClipName;
            }

            //ターゲットのキーだけいじり、他のクリップ状態については呼び出し元に責任を持ってもらう
            proxy.AccumulateValue(_latestKey, 1.0f);
            //表情を適用した = 目ボーンは正面向きになってほしい
            _eyeBoneResetter.ReserveReset = true;
        }
        

        private static BlendShapeKey CreateKey(string name) => 
            _presets.ContainsKey(name)
                ? new BlendShapeKey(_presets[name])
                : new BlendShapeKey(name);

        private static readonly Dictionary<string, BlendShapePreset> _presets = new Dictionary<string, BlendShapePreset>()
        {
            ["Blink"] = BlendShapePreset.Blink,
            ["Blink_L"] = BlendShapePreset.Blink_L,
            ["Blink_R"] = BlendShapePreset.Blink_R,

            ["LookLeft"] = BlendShapePreset.LookLeft,
            ["LookRight"] = BlendShapePreset.LookRight,
            ["LookUp"] = BlendShapePreset.LookUp,
            ["LookDown"] = BlendShapePreset.LookDown,
            
            ["A"] = BlendShapePreset.A,
            ["I"] = BlendShapePreset.I,
            ["U"] = BlendShapePreset.U,
            ["E"] = BlendShapePreset.E,
            ["O"] = BlendShapePreset.O,

            ["Joy"] = BlendShapePreset.Joy,
            ["Angry"] = BlendShapePreset.Angry,
            ["Sorrow"] = BlendShapePreset.Sorrow,
            ["Fun"] = BlendShapePreset.Fun,
        };
    }
}
