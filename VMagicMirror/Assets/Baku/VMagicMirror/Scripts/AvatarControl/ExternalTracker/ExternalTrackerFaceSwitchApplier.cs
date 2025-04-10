using UnityEngine;
using Zenject;
using Baku.VMagicMirror.ExternalTracker;
using UniRx;

namespace Baku.VMagicMirror
{
    public class ExternalTrackerFaceSwitchApplier : MonoBehaviour
    {
        private bool _hasModel = false;
        private FaceControlConfiguration _config;
        private ExternalTrackerDataSource _externalTracker;
        private EyeBoneAngleSetter _eyeBoneResetter;
        private string _latestClipName = "";

        private readonly ReactiveProperty<FaceSwitchKeyApplyContent> _currentValue
            = new(FaceSwitchKeyApplyContent.Empty());
        public IReadOnlyReactiveProperty<FaceSwitchKeyApplyContent> CurrentValue => _currentValue;


        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            FaceControlConfiguration config,
            ExternalTrackerDataSource externalTracker,
            EyeBoneAngleSetter eyeBoneResetter
            )
        {
            // 理由: FaceSwitchをExTracker以外からも発動可能にするよう仕様変更しているので
            Debug.Log("FaceSwitchUpdaterに以降してクラスごと削除したい");

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
                var key = ExpressionKeyUtils.CreateKeyByName(_externalTracker.FaceSwitchClipName);
                _currentValue.Value = FaceSwitchKeyApplyContent.Create(key, _externalTracker.KeepLipSyncForFaceSwitch, "");
                _latestClipName = _externalTracker.FaceSwitchClipName;
            }
        }

        public void Accumulate(ExpressionAccumulator accumulator)
        {
            //NOTE:
            //3つ目の条件について、表情間の補間処理中はこのクラスではないクラスがAccumulateを代行するので、
            //このクラスはWtMが有効なら表情は適用しないでOK
            if (!_hasModel || !_currentValue.HasValue)
            {
                return;
            }

            //ターゲットのキーだけいじり、他のクリップ状態については呼び出し元に責任を持ってもらう
            accumulator.Accumulate(_currentValue.Value.Key, 1f);
            //表情を適用した = 目ボーンは正面向きになってほしい
            _eyeBoneResetter.ReserveReset = true;
        }
    }
}
