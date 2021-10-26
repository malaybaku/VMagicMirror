using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    public class WordToMotionItemViewModel : ViewModelBase
    {
        public WordToMotionItemViewModel(WordToMotionSettingViewModel parent, MotionRequest model)
        {
            _parent = parent;
            MotionRequest = model;
            InitializeBuiltInClipNames();
            InitializeBlendShapeItems(parent);
            AvailableBuiltInClipNames =
                new ReadOnlyObservableCollection<string>(_availableBuiltInClipNames);
            BlendShapeItems =
                new ReadOnlyObservableCollection<BlendShapeItemViewModel>(_blendShapeItems);
            ExtraBlendShapeItems =
                new ReadOnlyObservableCollection<BlendShapeItemViewModel>(_extraBlendShapeItems);

            LoadFromModel(model);
        }

        private readonly WordToMotionSettingViewModel _parent;

        public ReadOnlyObservableCollection<string> AvailableCustomMotionClipNames => _parent.CustomMotionClipNames;

        /// <summary>ファイルI/Oや通信のベースになるデータを取得します。</summary>
        public MotionRequest MotionRequest { get; }

        /// <summary>
        /// 親オブジェクト側でブレンドシェイプを新規に取得したとき、そのブレンドシェイプ名を追加します。
        /// </summary>
        public void CheckBlendShapeClipNames()
        {
            foreach (var name in _parent
                .ExtraBlendShapeClipNames
                .Where(n => !_extraBlendShapeItems.Any(i => i.BlendShapeName == n))
                )
            {
                _extraBlendShapeItems.Add(new BlendShapeItemViewModel(
                    this,
                    name,
                    0,
                    _parent.LatestAvaterExtraClipNames.Contains(name)
                    ));
                MotionRequest?.ExtraBlendShapeValues.Add(new BlendShapePairItem()
                {
                    Name = name,
                    Value = 0
                });
            }
        }

        /// <summary>
        /// いま表示しているアバターが使っているブレンドシェイプ名を指定することで、
        /// どのクリップが実際に使われるかの情報を更新します。
        /// </summary>
        /// <param name="names"></param>
        public void CheckAvatarExtraClips()
        {
            var names = _parent.LatestAvaterExtraClipNames;
            foreach (var item in _extraBlendShapeItems)
            {
                item.IsUsedWithThisAvatar = names.Contains(item.BlendShapeName);
            }
        }

        /// <summary>
        /// 指定されたブレンドシェイプを忘れることを親オブジェクトにリクエストします。
        /// </summary>
        /// <param name="item"></param>
        public void RequestForgetClip(BlendShapeItemViewModel item) => _parent.ForgetClip(item);

        /// <summary>
        /// 指定された名称のブレンドシェイプ情報を忘れます。
        /// </summary>
        /// <param name="blendShapeName"></param>
        public void ForgetClip(string blendShapeName)
        {
            if (_extraBlendShapeItems
                .FirstOrDefault(i => i.BlendShapeName == blendShapeName)
                is BlendShapeItemViewModel item)
            {
                _extraBlendShapeItems.Remove(item);
            }

            if (MotionRequest
                ?.ExtraBlendShapeValues
                ?.FirstOrDefault(i => i.Name == blendShapeName)
                is BlendShapePairItem itemToRemove)
            {
                MotionRequest.ExtraBlendShapeValues.Remove(itemToRemove);
            }
        }

        //NOTE: ビューは同時に1つまでのItemしか表示しないので、コレだけで十分
        public RProperty<bool> EnablePreview => _parent.EnablePreview;

        private string _word = "";
        public string Word
        {
            get => _word;
            set => SetValue(ref _word, value);
        }

        //NOTE: この値はシリアライズ時に使うだけなのでgetter onlyでよく、変更通知も不要
        public int MotionType
        {
            get
            {
                return IsMotionTypeNone ? MotionRequest.MotionTypeNone :
                    IsMotionTypeBuiltInClip ? MotionRequest.MotionTypeBuiltInClip :
                    IsMotionTypeCustom ? MotionRequest.MotionTypeCustom :
                    MotionRequest.MotionTypeNone;
            }
        }

        private bool _isMotionTypeNone = true;
        public bool IsMotionTypeNone
        {
            get => _isMotionTypeNone;
            set
            {
                if (_isMotionTypeNone == value) { return; }

                if (value)
                {
                    IsMotionTypeBuiltInClip = false;
                    IsMotionTypeCustom = false;
                }
                _isMotionTypeNone = value;
                RaisePropertyChanged();
            }
        }

        private bool _isMotionTypeBuiltInClip = false;
        public bool IsMotionTypeBuiltInClip
        {
            get => _isMotionTypeBuiltInClip;
            set
            {
                if (_isMotionTypeBuiltInClip == value) { return; }

                if (value)
                {
                    IsMotionTypeNone = false;
                    IsMotionTypeCustom = false;
                }
                _isMotionTypeBuiltInClip = value;
                RaisePropertyChanged();
            }
        }

        private bool _isMotionTypeCustom = false;
        public bool IsMotionTypeCustom
        {
            get => _isMotionTypeCustom;
            set
            {
                if (_isMotionTypeCustom == value) { return; }
                if (value)
                {
                    IsMotionTypeNone = false;
                    IsMotionTypeBuiltInClip = false;
                }
                _isMotionTypeCustom = value;
                RaisePropertyChanged();
            }
        }

        private string _builtInClipName = "";
        public string BuiltInClipName
        {
            get => _builtInClipName;
            set => SetValue(ref _builtInClipName, value);
        }

        private string _customMotionClipName = "";
        public string CustomMotionClipName
        {
            get => _customMotionClipName;
            set => SetValue(ref _customMotionClipName, value);
        }

        private bool _useBlendShape = false;
        /// <summary>このアイテムがブレンドシェイプの変更要求を含んでいるかどうかを取得、設定します。</summary>
        public bool UseBlendShape
        {
            get => _useBlendShape;
            set => SetValue(ref _useBlendShape, value);
        }

        private bool _holdBlendShape = false;
        public bool HoldBlendShape
        {
            get => _holdBlendShape;
            set => SetValue(ref _holdBlendShape, value);
        }

        private bool _preferLipSync = false;
        public bool PreferLipSync
        {
            get => _preferLipSync;
            set => SetValue(ref _preferLipSync, value);
        }


        private float _durationWhenOnlyBlendShape = 3.0f;
        public float DurationWhenOnlyBlendShape
        {
            get => _durationWhenOnlyBlendShape;
            set => SetValue(ref _durationWhenOnlyBlendShape, value);
        }

        public ReadOnlyObservableCollection<BlendShapeItemViewModel> BlendShapeItems { get; }
        private ObservableCollection<BlendShapeItemViewModel> _blendShapeItems
            = new ObservableCollection<BlendShapeItemViewModel>();

        public ReadOnlyObservableCollection<BlendShapeItemViewModel> ExtraBlendShapeItems { get; }
        private ObservableCollection<BlendShapeItemViewModel> _extraBlendShapeItems
            = new ObservableCollection<BlendShapeItemViewModel>();

        #region Commands

        private ActionCommand? _moveUpCommand;
        public ActionCommand MoveUpCommand
            => _moveUpCommand ??= new ActionCommand(() => _parent.MoveUpItem(this));

        private ActionCommand? _moveDownCommand;
        public ActionCommand MoveDownCommand
            => _moveDownCommand ??= new ActionCommand(() => _parent.MoveDownItem(this));

        private ActionCommand? _editCommand;
        public ActionCommand EditCommand
            => _editCommand ??= new ActionCommand(() => _parent.EditItemByDialog(this));

        private ActionCommand? _playCommand;
        public ActionCommand PlayCommand
            => _playCommand ??= new ActionCommand(() => _parent.Play(this));

        private ActionCommand? _deleteCommand;
        public ActionCommand DeleteCommand
            => _deleteCommand ??= new ActionCommand(() => _parent.DeleteItem(this));

        private ActionCommand? _openWordToMotionCustomHowToCommand;
        public ActionCommand OpenWordToMotionCustomHowToCommand
            => _openWordToMotionCustomHowToCommand ??= new ActionCommand(() => UrlNavigate.Open(
                "https://malaybaku.github.io/VMagicMirror/tips/use_custom_motion"
                ));

        private ActionCommand? _checkCustomMotionDataValidityCommand;
        public ActionCommand CheckCustomMotionDataValidityCommand
            => _checkCustomMotionDataValidityCommand ??= new ActionCommand(
                () => _parent.RequestCustomMotionDoctor()
                );

        #endregion

        private ObservableCollection<string> _availableBuiltInClipNames
            = new ObservableCollection<string>();
        public ReadOnlyObservableCollection<string> AvailableBuiltInClipNames { get; }

        /// <summary>ViewModelの変更を破棄してモデルの値を取得し直します。</summary>
        public void ResetChanges() => LoadFromModel(MotionRequest);

        /// <summary>変更内容を確定し、モデルクラスにデータを書き込みます。</summary>
        public void SaveChanges() => WriteToModel(MotionRequest);

        public void WriteToModel(MotionRequest? model)
        {
            if (model == null) { return; }
            model.Word = Word;
            model.MotionType = MotionType;

            model.BuiltInAnimationClipName = BuiltInClipName;
            model.CustomMotionClipName = CustomMotionClipName;
            model.UseBlendShape = UseBlendShape;
            model.HoldBlendShape = HoldBlendShape;
            model.PreferLipSync = PreferLipSync;

            model.DurationWhenOnlyBlendShape = DurationWhenOnlyBlendShape;

            model.BlendShapeValues.Clear();
            foreach (var item in BlendShapeItems)
            {
                model.BlendShapeValues[item.BlendShapeName] = item.ValuePercentage;
            }

            model.ExtraBlendShapeValues.Clear();
            foreach (var item in ExtraBlendShapeItems)
            {

                model.ExtraBlendShapeValues.Add(new BlendShapePairItem()
                {
                    Name = item.BlendShapeName,
                    Value = item.ValuePercentage,
                });
            }
        }

        private void LoadFromModel(MotionRequest? model)
        {
            if (model == null) { return; }
            Word = model.Word;

            switch (model.MotionType)
            {
                case MotionRequest.MotionTypeNone:
                    IsMotionTypeNone = true;
                    break;
                case MotionRequest.MotionTypeBuiltInClip:
                    IsMotionTypeBuiltInClip = true;
                    break;
                case MotionRequest.MotionTypeCustom:
                    IsMotionTypeCustom = true;
                    break;
                default:
                    IsMotionTypeNone = true;
                    break;
            }

            BuiltInClipName = model.BuiltInAnimationClipName;
            CustomMotionClipName = model.CustomMotionClipName;

            UseBlendShape = model.UseBlendShape;
            HoldBlendShape = model.HoldBlendShape;
            PreferLipSync = model.PreferLipSync;
            DurationWhenOnlyBlendShape = model.DurationWhenOnlyBlendShape;

            foreach (var blendShapeItem in model.BlendShapeValues)
            {
                var item = BlendShapeItems.FirstOrDefault(i => i.BlendShapeName == blendShapeItem.Key);
                if (item != null)
                {
                    item.ValuePercentage = blendShapeItem.Value;
                }
            }

            foreach (var blendShapeItem in model.ExtraBlendShapeValues)
            {
                var item = ExtraBlendShapeItems.FirstOrDefault(i => i.BlendShapeName == blendShapeItem.Name);
                if (item != null)
                {
                    item.ValuePercentage = blendShapeItem.Value;
                }
            }
        }

        private void InitializeBuiltInClipNames()
        {
            //NOTE: 数が少ないのでハードコーディングで済ます
            _availableBuiltInClipNames.Add("Wave");
            _availableBuiltInClipNames.Add("Rokuro");
            _availableBuiltInClipNames.Add("Good");
            _availableBuiltInClipNames.Add("Nod");
            _availableBuiltInClipNames.Add("Shake");
        }

        private void InitializeBlendShapeItems(WordToMotionSettingViewModel parent)
        {
            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "Joy", 0));
            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "Angry", 0));
            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "Sorrow", 0));
            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "Fun", 0));

            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "A", 0));
            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "I", 0));
            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "U", 0));
            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "E", 0));
            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "O", 0));

            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "Neutral", 0));
            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "Blink", 0));
            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "Blink_L", 0));
            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "Blink_R", 0));

            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "LookUp", 0));
            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "LookDown", 0));
            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "LookLeft", 0));
            _blendShapeItems.Add(new BlendShapeItemViewModel(this, "LookRight", 0));

            foreach (var name in parent.ExtraBlendShapeClipNames)
            {
                _extraBlendShapeItems.Add(new BlendShapeItemViewModel(
                    this,
                    name,
                    0,
                    parent.LatestAvaterExtraClipNames.Contains(name)
                    ));
            }
        }
    }
}
