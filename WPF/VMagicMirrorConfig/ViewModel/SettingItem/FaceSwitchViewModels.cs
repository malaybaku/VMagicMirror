using System.Collections.ObjectModel;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    /// <summary> 表情スイッチの個別アイテムのビューモデル </summary>
    public class ExternalTrackerFaceSwitchItemViewModel : ViewModelBase
    {
        public ExternalTrackerFaceSwitchItemViewModel(ExternalTrackerViewModel parent, ExternalTrackerFaceSwitchItem model)
        {
            _parent = parent;
            _model = model;
            RefreshInstruction();
        }

        private void RefreshInstruction() => Instruction = GetInstructionText(_model.SourceName);

        internal void SubscribeLanguageSelector()
            => LanguageSelector.Instance.LanguageChanged += RefreshInstruction;

        internal void UnsubscribeLanguageSelector()
            => LanguageSelector.Instance.LanguageChanged -= RefreshInstruction;

        private readonly ExternalTrackerViewModel _parent;
        private readonly ExternalTrackerFaceSwitchItem _model;

        #region 保存しないでよい値

        /// <summary> 
        /// "この表情のパラメタがN%以上になったら"みたいなしきい値の取りうる値。
        /// 細かく設定できる意味がないので10%刻みです。
        /// </summary>
        public ThresholdItem[] AvailablePercentages { get; } = Enumerable
            .Range(1, 9)
            .Select(i => new ThresholdItem(i * 10, $"{i * 10}%"))
            .ToArray();

        public ReadOnlyObservableCollection<string> BlendShapeNames => _parent.BlendShapeNames;

        private string _instruction = "";
        public string Instruction
        {
            get => _instruction;
            set => SetValue(ref _instruction, value);
        }

        #endregion

        #region 保存すべき値

        public int Threshold
        {
            get => _model.ThresholdPercent;
            set
            {
                if (_model.ThresholdPercent != value)
                {
                    _model.ThresholdPercent = value;
                    RaisePropertyChanged();
                    _parent.SaveFaceSwitchSetting();
                }
            }
        }

        public string ClipName
        {
            get => _model.ClipName;
            set
            {
                if (_model.ClipName != value)
                {
                    _model.ClipName = value;
                    RaisePropertyChanged();
                    _parent.RefreshUsedBlendshapeNames();
                    _parent.SaveFaceSwitchSetting();
                }
            }
        }

        public bool KeepLipSync
        {
            get => _model.KeepLipSync;
            set
            {
                if (_model.KeepLipSync != value)
                {
                    _model.KeepLipSync = value;
                    RaisePropertyChanged();
                    _parent.SaveFaceSwitchSetting();
                }
            }
        }

        #endregion

        public class ThresholdItem
        {
            public ThresholdItem(int value, string text)
            {
                Value = value;
                Text = text;
            }

            public int Value { get; }
            public string Text { get; }
        }

        //NOTE: keyはWPFコード内で決め打ちしたものしか来ないはずなため、"-"にはならないはず(なったらコードのバグ)
        private static string GetInstructionText(string key)
        {
            var result = LocalizedString.GetString("ExTracker_FaceSwitch_Trigger_" + key);
            return string.IsNullOrEmpty(result) ? "-" : result;
        }
    }
}
