using System.Collections.Generic;
using UnityEngine;
using Zenject;
using VRM;
using Baku.VMagicMirror.ExternalTracker;
using UniRx;

namespace Baku.VMagicMirror
{
    public readonly struct FaceSwitchKeyApplyContent
    {
        private FaceSwitchKeyApplyContent(bool hasValue, bool keepLipSync, BlendShapeKey key)
        {
            HasValue = hasValue;
            KeepLipSync = keepLipSync;
            Key = key;
        }

        public static FaceSwitchKeyApplyContent Empty() => new FaceSwitchKeyApplyContent(false, false, default);

        public static FaceSwitchKeyApplyContent Create(BlendShapeKey key, bool keepLipSync) =>
            new FaceSwitchKeyApplyContent(true, keepLipSync, key);
        
        public bool HasValue { get; }
        public bool KeepLipSync { get; }
        public BlendShapeKey Key { get; }
    }
        
    public class ExternalTrackerFaceSwitchApplier : MonoBehaviour
    {
        private bool _hasModel = false;
        private FaceControlConfiguration _config;
        private ExternalTrackerDataSource _externalTracker;
        private EyeBoneAngleSetter _eyeBoneResetter;
        private string _latestClipName = "";

        private readonly ReactiveProperty<FaceSwitchKeyApplyContent> _currentValue 
            = new ReactiveProperty<FaceSwitchKeyApplyContent>(FaceSwitchKeyApplyContent.Empty());
        public IReadOnlyReactiveProperty<FaceSwitchKeyApplyContent> CurrentValue => _currentValue;


        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            FaceControlConfiguration config,
            ExternalTrackerDataSource externalTracker,
            EyeBoneAngleSetter eyeBoneResetter
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
                _currentValue.Value = FaceSwitchKeyApplyContent.Empty();
                _latestClipName = "";
            };
        }
        
        //public bool HasClipToApply => !string.IsNullOrEmpty(_externalTracker.FaceSwitchClipName);
        public bool KeepLipSync => _currentValue.Value.KeepLipSync;
        public bool HasClipToApply => _currentValue.Value.HasValue;

        public void UpdateCurrentValue()
        {
            //NOTE: FaceSwitchClipNameはnullにはならないという前提で実装している
            if (!_hasModel || _latestClipName == _externalTracker.FaceSwitchClipName)
            {
                return;
            }

            if (string.IsNullOrEmpty(_externalTracker.FaceSwitchClipName))
            {
                _currentValue.Value = FaceSwitchKeyApplyContent.Empty();
                _latestClipName = "";
            }
            else
            {
                var key = CreateKey(_externalTracker.FaceSwitchClipName);
                _currentValue.Value = FaceSwitchKeyApplyContent.Create(key, _externalTracker.KeepLipSyncForFaceSwitch);
                _latestClipName = _externalTracker.FaceSwitchClipName;
            }
        }

        public void Accumulate(VRMBlendShapeProxy proxy)
        {
            //NOTE:
            //3つ目の条件について、表情間の補間処理中はこのクラスではないクラスがAccumulateを代行するので、
            //このクラスはWtMが有効なら表情は適用しないでOK
            if (!_hasModel || !_currentValue.HasValue)
            {
                return;
            }

            //ターゲットのキーだけいじり、他のクリップ状態については呼び出し元に責任を持ってもらう
            proxy.AccumulateValue(_currentValue.Value.Key, 1f);
            //表情を適用した = 目ボーンは正面向きになってほしい
            _eyeBoneResetter.ReserveReset = true;
        }

        private static BlendShapeKey CreateKey(string name) => 
            _presets.ContainsKey(name)
                ? BlendShapeKey.CreateFromPreset(_presets[name])
                : BlendShapeKey.CreateUnknown(name);

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
