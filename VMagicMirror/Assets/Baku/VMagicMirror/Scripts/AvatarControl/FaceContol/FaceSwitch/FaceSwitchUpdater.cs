using System;
using System.Linq;
using Baku.VMagicMirror.ExternalTracker;
using UniRx;
using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    public readonly struct FaceSwitchKeyApplyContent
    {
        private FaceSwitchKeyApplyContent(bool hasValue, bool keepLipSync, ExpressionKey key, string accessoryName)
        {
            HasValue = hasValue;
            KeepLipSync = keepLipSync;
            Key = key;
            AccessoryName = accessoryName;
        }

        //NOTE: default指定してName == nullになってると怒られる事があるので便宜的にneutralで代用している
        public static FaceSwitchKeyApplyContent Empty()
            => new(false, false, ExpressionKey.Neutral, "");

        public static FaceSwitchKeyApplyContent Create(ExpressionKey key, bool keepLipSync, string accessoryName) =>
            new(true, keepLipSync, key, accessoryName);
        
        public bool HasValue { get; }
        public bool KeepLipSync { get; }
        public ExpressionKey Key { get; }
        public string AccessoryName { get; }
    }

    public class FaceSwitchUpdater : PresenterBase
    {
        //いちど適用したFaceSwitchは最小でもこの秒数だけ維持するよ、という下限値。チャタリングを防ぐのが狙い。
        private const float FaceSwitchMinimumKeepDuration = 0.5f;

        private readonly IVRMLoadable _vrmLoadable;
        private readonly IMessageReceiver _messageReceiver;
        private readonly FaceSwitchExtractor _faceSwitch;
        private readonly FaceControlConfiguration _config;
        private readonly ExternalTrackerDataSource _externalTracker;
        private readonly EyeBoneAngleSetter _eyeBoneSetter;

        [Inject]
        public FaceSwitchUpdater(
            IVRMLoadable vrmLoadable,
            IMessageReceiver messageReceiver,
            FaceSwitchExtractor faceSwitch,
            FaceControlConfiguration config,
            ExternalTrackerDataSource externalTracker,
            EyeBoneAngleSetter eyeBoneSetter)
        {
            _vrmLoadable = vrmLoadable;
            _messageReceiver = messageReceiver;
            _faceSwitch = faceSwitch;
            _config = config;
            _externalTracker = externalTracker;
            _eyeBoneSetter = eyeBoneSetter;
        }
        
        private bool _hasModel;
        private float _faceSwitchKeepCount;

        private readonly ReactiveProperty<FaceSwitchKeyApplyContent> _currentValue
            = new(FaceSwitchKeyApplyContent.Empty());
        public IReadOnlyReactiveProperty<FaceSwitchKeyApplyContent> CurrentValue => _currentValue;
        
        private readonly ReactiveProperty<ActiveFaceSwitchItem> _activeFaceSwitchItem = new();
        public IReadOnlyReactiveProperty<ActiveFaceSwitchItem> ActiveFaceSwitchItem => _activeFaceSwitchItem;

        
        //public bool HasClipToApply => !string.IsNullOrEmpty(_externalTracker.FaceSwitchClipName);
        public bool KeepLipSync => _currentValue.Value.KeepLipSync;
        public bool HasClipToApply => _currentValue.Value.HasValue;

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmUnloaded;
            
            _messageReceiver.AssignCommandHandler(
                VmmCommands.ExTrackerSetFaceSwitchSetting,
                m => SetFaceSwitchSetting(m.Content)
                );
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            var expressionKeys = info.instance.Vrm.Expression.LoadExpressionMap().Keys;
            _faceSwitch.AvatarBlendShapeNames = expressionKeys.Select(k =>
                {
                    var result = k.Name;
                    if (k.Preset != ExpressionPreset.custom)
                    {
                        result = char.ToUpper(result[0]) + result.Substring(1);
                    }
                    return result;
                })
                .ToArray();
            _hasModel = true;
        }

        private void OnVrmUnloaded()
        {
            _hasModel = false;
            _currentValue.Value = FaceSwitchKeyApplyContent.Empty();
            _faceSwitch.AvatarBlendShapeNames = Array.Empty<string>();
        }

        private void SetFaceSwitchSetting(string json)
        {
            try
            {
                _faceSwitch.Setting = JsonUtility.FromJson<FaceSwitchSettings>(json);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
        
        // NOTE: タイミングが重要なのでTickでは実装してないが、毎フレーム呼ばれる想定
        public void UpdateCurrentValue(float deltaTime)
        {
            if (!_hasModel)
            {
                _faceSwitch.ResetUpdateCalledFlag();
                return;
            }

            // 書いてる通りだが、チャタリングしないようにしたうえでFaceSwitchの現在有効な値を切り替える
            if (_faceSwitchKeepCount > 0)
            {
                _faceSwitchKeepCount -= deltaTime;
            }

            if (_faceSwitchKeepCount <= 0 && !ActiveFaceSwitchItem.Value.Equals(_faceSwitch.ActiveItem))
            {
                _activeFaceSwitchItem.Value = _faceSwitch.ActiveItem;
                _faceSwitchKeepCount = FaceSwitchMinimumKeepDuration;
            }
            
            // NOTE:
            // 誰もUpdateを呼んでない場合、トラッキングロストしている or Webカメラ等が完全に止まってるはずなので、
            // その場合も何も適用しない
            if (!_faceSwitch.UpdateCalled || _activeFaceSwitchItem.Value.IsEmpty)
            {
                _currentValue.Value = FaceSwitchKeyApplyContent.Empty();
                return;
            }

            _faceSwitch.ResetUpdateCalledFlag();

            var activeItem = _activeFaceSwitchItem.Value;
            var key = ExpressionKeyUtils.CreateKeyByName(activeItem.ClipName);
            _currentValue.Value = FaceSwitchKeyApplyContent.Create(
                key,
                activeItem.KeepLipSync,
                activeItem.AccessoryName
            );
        }

        /// <summary>
        /// NOTE: FaceSwitchの結果への補間が必要なケースでは <see cref="BlendShapeInterpolator"/> が補間を代行するので、
        /// このクラスとしてはweightつきのAccumulate関数は不要
        /// </summary>
        /// <param name="accumulator"></param>
        public void Accumulate(ExpressionAccumulator accumulator)
        {
            if (!_hasModel || !_currentValue.HasValue)
            {
                return;
            }

            //ターゲットのキーだけいじり、他のクリップ状態については呼び出し元に責任を持ってもらう
            accumulator.Accumulate(_currentValue.Value.Key, 1f);
            //表情を適用した = 目ボーンは正面向きになってほしい
            _eyeBoneSetter.ReserveReset = true;
        }
    }
}
