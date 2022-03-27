using Baku.VMagicMirrorConfig.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    public class WordToMotionSettingViewModel : SettingViewModelBase
    {
        public WordToMotionSettingViewModel() : this(
            ModelResolver.Instance.Resolve<WordToMotionSettingModel>(),
            ModelResolver.Instance.Resolve<LayoutSettingModel>(),
            ModelResolver.Instance.Resolve<AccessorySettingModel>(),
            ModelResolver.Instance.Resolve<CustomMotionList>(),
            ModelResolver.Instance.Resolve<WordToMotionRuntimeConfig>()
            )
        {
        }

        internal WordToMotionSettingViewModel(
            WordToMotionSettingModel model,
            LayoutSettingModel layoutModel,
            AccessorySettingModel accessoryModel,
            CustomMotionList customMotionList,
            WordToMotionRuntimeConfig extraBlendShapeNames
            )
        {
            _model = model;
            _layoutModel = layoutModel;
            _customMotionList = customMotionList;
            _runtimeConfigModel = extraBlendShapeNames;

            Items = new ReadOnlyObservableCollection<WordToMotionItemViewModel>(_items);
            Devices = WordToMotionDeviceItemViewModel.LoadAvailableItems();
            AvailableAccessoryNames = new AccessoryItemNamesViewModel(accessoryModel);

            AddNewItemCommand = new ActionCommand(() => _model.AddNewItem());
            OpenKeyAssignmentEditorCommand = new ActionCommand(() => OpenKeyAssignmentEditor());
            ResetByDefaultItemsCommand = new ActionCommand(
                () => SettingResetUtils.ResetSingleCategoryAsync(_runtimeConfigModel.LoadDefaultItems)
                );

            if (IsInDegignMode)
            {
                return;
            }

            SelectedDevice = Devices.FirstOrDefault(d => d.Index == _model.SelectedDeviceType.Value);
            _model.SelectedDeviceType.AddWeakEventHandler(OnSelectedDeviceTypeChanged);

            //NOTE: シリアライズ文字列はどのみち頻繁に更新せねばならない
            //(並び替えた時とかもUnityにデータ送るために更新がかかる)ので、そのタイミングを使う
            WeakEventManager<WordToMotionSettingModel, EventArgs>.AddHandler(
                _model,
                nameof(_model.MidiNoteToMotionMapReloaded),
                OnMidiNoteToMotionMapReloaded
                );
            WeakEventManager<WordToMotionSettingModel, EventArgs>.AddHandler(
                _model,
                nameof(_model.MotionRequestsReloaded),
                OnMotionRequestsReloaded
                );

            WeakEventManager<WordToMotionSettingModel, EventArgs>.AddHandler(
                _model,
                nameof(_model.Loaded),
                OnSettingLoaded);

            WeakEventManager<WordToMotionItemPreviewDataSender, EventArgs>.AddHandler(
                _model.PreviewDataSender,
                nameof(_model.PreviewDataSender.PrepareDataSend),
                OnPrepareDataSend
                );

            WeakEventManager<WordToMotionRuntimeConfig, BlendShapeCheckedEventArgs>.AddHandler(
                _runtimeConfigModel,
                nameof(_runtimeConfigModel.DetectNewExtraBlendShapeName),
                OnBlendShapeChecked
                );

            LoadMotionItems();
            LoadMidiSettingItems();
        }

        

        private readonly WordToMotionSettingModel _model;
        private readonly LayoutSettingModel _layoutModel;
        private readonly WordToMotionRuntimeConfig _runtimeConfigModel;
        private readonly CustomMotionList _customMotionList;
        private WordToMotionItemViewModel? _dialogItem;

        public AccessoryItemNamesViewModel AvailableAccessoryNames { get; }

        private void OnSelectedDeviceTypeChanged(object? sender, PropertyChangedEventArgs e)
        {
            SelectedDevice = Devices.FirstOrDefault(d => d.Index == _model.SelectedDeviceType.Value);
            EnableWordToMotion.Value = _model.SelectedDeviceType.Value != WordToMotionSetting.DeviceTypes.None;
        }

        private void OnSettingLoaded(object? sender, EventArgs e)
        {
            LoadMidiSettingItems();
            LoadMotionItems();
        }

        private void OnMidiNoteToMotionMapReloaded(object? sender, EventArgs e)
        {
            if (!_model.IsLoading)
            {
                LoadMidiSettingItems();
            }
        }

        private void OnMotionRequestsReloaded(object? sender, EventArgs e)
        {
            if (!_model.IsLoading)
            {
                LoadMotionItems();
            }
        }

        private void OnBlendShapeChecked(object? sender, BlendShapeCheckedEventArgs e)
        {
            if (e.HasNewBlendShape)
            {
                //新しい名称のクリップを子要素側に反映
                foreach (var item in _items)
                {
                    item.CheckBlendShapeClipNames();
                }
            }

            foreach (var item in _items)
            {
                item.CheckAvatarExtraClips();
            }
        }

        private void OnPrepareDataSend(object? sender, EventArgs e)
        {
            _dialogItem?.WriteToModel(_model.PreviewDataSender.MotionRequest);
        }

        public ReadOnlyObservableCollection<string> CustomMotionClipNames => _customMotionList.CustomMotionClipNames;

        public RProperty<bool> EnableWordToMotion { get; } = new RProperty<bool>(true);

        #region デバイスをWord to Motionに割り当てる設定

        public WordToMotionDeviceItemViewModel[] Devices { get; }

        private WordToMotionDeviceItemViewModel? _selectedDevice = null;
        public WordToMotionDeviceItemViewModel? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (_selectedDevice == value)
                {
                    return;
                }
                _selectedDevice = value;
                RaisePropertyChanged();
                _model.SelectedDeviceType.Value = _selectedDevice?.Index ?? WordToMotionSetting.DeviceTypes.None;
            }
        }
        public RProperty<int> SelectedDeviceType => _model.SelectedDeviceType;

        #endregion

        public List<string> ExtraBlendShapeClipNames => _runtimeConfigModel.ExtraBlendShapeClipNames;
        public string[] LatestAvaterExtraClipNames => _runtimeConfigModel.LatestAvaterExtraClipNames;

        public ReadOnlyObservableCollection<WordToMotionItemViewModel> Items { get; }
        private readonly ObservableCollection<WordToMotionItemViewModel> _items = new();
        public MidiNoteToMotionMapViewModel MidiNoteMap { get; }
            = new MidiNoteToMotionMapViewModel(MidiNoteToMotionMap.LoadDefault());

        public RProperty<bool> EnablePreview => _model.EnablePreview;

        /// <summary>Word to Motionのアイテム編集を開始した時すぐプレビューを開始するかどうか。普通は即スタートでよい</summary>
        public bool EnablePreviewWhenStartEdit { get; set; } = true;

        private void LoadMotionItems()
        {
            _items.Clear();

            var modelItems = _model.MotionRequests.Requests;
            if (modelItems.Length == 0)
            {
                return;
            }

            foreach (var item in modelItems)
            {
                if (item == null)
                {
                    //一応チェックしてるけど本来nullはあり得ない
                    LogOutput.Instance.Write("Receive null MotionRequest");
                    continue;
                }

                //NOTE: 前処理として、この時点で読み込んだモデルに不足なExtraClipがある場合は差し込んでおく
                //これは異バージョンとか考慮した処理です
                foreach (var extraClip in ExtraBlendShapeClipNames)
                {
                    if (!item.ExtraBlendShapeValues.Any(i => i.Name == extraClip))
                    {
                        item.ExtraBlendShapeValues.Add(new BlendShapePairItem()
                        {
                            Name = extraClip,
                            Value = 0,
                        });
                    }
                }

                _items.Add(new WordToMotionItemViewModel(this, item));
            }
        }

        private void LoadMidiSettingItems()
        {
            MidiNoteMap.Load(_model.MidiNoteToMotionMap);
        }

        /// <summary>
        /// <see cref="ItemsContentString"/>に、現在の<see cref="Items"/>の内容をシリアライズした文字列を設定します。
        /// </summary>
        public void SaveItems() => _model.RequestSerializeItems();

        public void Play(WordToMotionItemViewModel item) => _model.Play(item.MotionRequest);

        public void MoveUpItem(WordToMotionItemViewModel item) => _model.MoveUpItem(item.MotionRequest);
        public void MoveDownItem(WordToMotionItemViewModel item) => _model.MoveDownItem(item.MotionRequest);
        //NOTE: 用途的にここでTaskを切る(Modelのレベルで切ると不健全だからね.)
        public async void DeleteItem(WordToMotionItemViewModel item) => await _model.DeleteItem(item.MotionRequest);

        /// <summary>
        /// 確認ダイアログを出したのち，指定されたブレンドシェイプをアプリの設定から除去します。
        /// </summary>
        /// <param name="blendShapeItem"></param>
        public async void ForgetClip(BlendShapeItemViewModel blendShapeItem)
        {
            string name = blendShapeItem.BlendShapeName;
            var indication = MessageIndication.ForgetBlendShapeClip();
            bool res = await MessageBoxWrapper.Instance.ShowAsyncOnWordToMotionItemEdit(
                indication.Title,
                string.Format(indication.Content, name)
                );
            if (res)
            {
                foreach (var item in _items)
                {
                    item.ForgetClip(name);
                }

                if (ExtraBlendShapeClipNames.Contains(name))
                {
                    ExtraBlendShapeClipNames.Remove(name);
                }
                RequestReload();
            }
        }

        //NOTE: この結果シリアライズ文字列が変わると、モデル側でメッセージ送信もやってくれる
        /// <summary>モーション一覧の情報が変わったとき、Unity側に再読み込みをリクエストします。</summary>
        public void RequestReload() => SaveItems();

        public ActionCommand OpenKeyAssignmentEditorCommand { get; }

        private async void OpenKeyAssignmentEditor()
        {
            //note: 今のところMIDIコン以外は割り当て固定です
            if (_model.SelectedDeviceType.Value != WordToMotionSetting.DeviceTypes.MidiController)
            {
                return;
            }

            if (!_layoutModel.EnableMidiRead.Value)
            {
                //MIDIの読み取りが無効だと設定ウィンドウの意味がない(どうせMIDIに反応できない)の確認
                bool enableMidi = await MessageBoxWrapper.Instance.ShowAsync(
                    LocalizedString.GetString("WordToMotion_MidiAssign_MidiNotActive_Title"),
                    LocalizedString.GetString("WordToMotion_MidiAssign_MidiNotActive_Message"),
                    MessageBoxWrapper.MessageBoxStyle.OKCancel
                    );
                   
                if (enableMidi)
                {
                    _layoutModel.EnableMidiRead.Value = true;
                }
                else
                {
                    return;
                }
            }

            var vm = new MidiNoteToMotionEditorViewModel(MidiNoteMap, _model.MidiNoteReceiver);

            _model.RequireMidiNoteOnMessage(true);
            var window = new MidiNoteAssignEditorWindow()
            {
                DataContext = vm,
            };
            bool? res = window.ShowDialog();
            _model.RequireMidiNoteOnMessage(false);

            if (res == true)
            {
                _model.RefreshMidiNoteMap(vm.Result);
            }
        }

        public ActionCommand AddNewItemCommand { get; }

        public ActionCommand ResetByDefaultItemsCommand { get; }
      

        public void EditItemByDialog(WordToMotionItemViewModel item)
        {
            var dialog = new WordToMotionItemEditWindow()
            {
                DataContext = item,
                Owner = SettingWindow.CurrentWindow,
            };

            _dialogItem = item;

            EnablePreview.Value = EnablePreviewWhenStartEdit;

            if (dialog.ShowDialog() == true)
            {
                item.SaveChanges();
                RequestReload();
            }
            else
            {
                item.ResetChanges();
            }

            EnablePreviewWhenStartEdit = EnablePreview.Value;
            EnablePreview.Value = false;
            _dialogItem = null;
        }

        public void RequestCustomMotionDoctor() => _model.RequestCustomMotionDoctor();
    }
}
