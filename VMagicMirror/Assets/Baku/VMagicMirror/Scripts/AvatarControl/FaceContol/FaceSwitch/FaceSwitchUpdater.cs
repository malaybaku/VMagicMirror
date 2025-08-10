using System;
using System.Linq;
using R3;
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
        private readonly EyeBoneAngleSetter _eyeBoneSetter;

        [Inject]
        public FaceSwitchUpdater(
            IVRMLoadable vrmLoadable,
            IMessageReceiver messageReceiver,
            FaceSwitchExtractor faceSwitch,
            FaceControlConfiguration config,
            EyeBoneAngleSetter eyeBoneSetter)
        {
            _vrmLoadable = vrmLoadable;
            _messageReceiver = messageReceiver;
            _faceSwitch = faceSwitch;
            _config = config;
            _eyeBoneSetter = eyeBoneSetter;
        }
        
        private bool _hasModel;
        private float _faceSwitchKeepCount;

        /// <summary>
        /// <see cref="FaceSwitchExtractor.ActiveItem"/> に対してチャタリング防止を加味した現在のFaceSwitchの値
        /// </summary>
        private ActiveFaceSwitchItem _faceSwitchItem = ActiveFaceSwitchItem.Empty;

        // NOTE: 上記の値に対してExpressionKeyへの変換、およびトラッキングロスしたケースもケアした、公開可能なFaceSwitchの値
        private readonly ReactiveProperty<FaceSwitchKeyApplyContent> _currentValue
            = new(FaceSwitchKeyApplyContent.Empty());
        public ReadOnlyReactiveProperty<FaceSwitchKeyApplyContent> CurrentValue => _currentValue;
        
        public bool HasClipToApply => _currentValue.Value.HasValue;
        // NOTE: `HasValue &&` もチェックしたほうがロバストだが、冗長なはずなのでやってない
        public bool KeepLipSync => _currentValue.Value.KeepLipSync;

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmUnloaded;
            
            _messageReceiver.AssignCommandHandler(
                VmmCommands.ExTrackerSetFaceSwitchSetting,
                m => SetFaceSwitchSetting(m.GetStringValue())
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
            _faceSwitch.AvatarBlendShapeNames = Array.Empty<string>();
            _faceSwitchItem = ActiveFaceSwitchItem.Empty;
            _currentValue.Value = FaceSwitchKeyApplyContent.Empty();
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
        
        // NOTE: タイミングが重要なのでTickでは実装してないが、毎フレーム呼ばれる想定
        public void UpdateCurrentValue(float deltaTime)
        {
            UpdateCurrentValueInternal(deltaTime);
            _config.FaceSwitchActive = _currentValue.Value.HasValue;
        }

        private void UpdateCurrentValueInternal(float deltaTime)
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

            if (_faceSwitchKeepCount <= 0 && !_faceSwitchItem.Equals(_faceSwitch.ActiveItem))
            {
                _faceSwitchItem = _faceSwitch.ActiveItem;
                _faceSwitchKeepCount = FaceSwitchMinimumKeepDuration;
            }
            
            // NOTE:
            // 誰もUpdateを呼んでない場合、トラッキングロストしている or Webカメラ等が完全に止まってるはずなので、
            // その場合も何も適用しない
            if (_faceSwitch.UpdateCalled && !_faceSwitchItem.IsEmpty)
            {
                _currentValue.Value = FaceSwitchKeyApplyContent.Create(
                    ExpressionKeyUtils.CreateKeyByName(_faceSwitchItem.ClipName),
                    _faceSwitchItem.KeepLipSync,
                    _faceSwitchItem.AccessoryName
                );
            }
            else
            {
                _currentValue.Value = FaceSwitchKeyApplyContent.Empty();
            }

            _faceSwitch.ResetUpdateCalledFlag();
        }
    }
}
