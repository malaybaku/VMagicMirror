using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace Baku.VMagicMirrorConfig.ViewModel
{
    // Buddy全体の設定のVM。個別のBuddy設定に加えて、全Buddyに共通の設定もここに入る
    public class BuddySettingViewModel : ViewModelBase
    {
        public BuddySettingViewModel() : this(
            ModelResolver.Instance.Resolve<BuddySettingModel>(),
            ModelResolver.Instance.Resolve<BuddySettingsSender>())
        {
        }

        internal BuddySettingViewModel(BuddySettingModel model, BuddySettingsSender buddySettingsSender)
        {
            _model = model;
            _buddySettingsSender = buddySettingsSender;
            Items = new ReadOnlyObservableCollection<BuddyItemViewModel>(_items);

            OpenBuddyFolderCommand = new ActionCommand(OpenBuddyFolder);
            OpenDocUrlCommand = new ActionCommand(OpenDocUrl);
            ReloadAllCommand = new ActionCommand(() => _model.ReloadAll());

            if (!IsInDesignMode)
            {
                WeakEventManager<BuddySettingModel, EventArgs>.AddHandler(_model, nameof(_model.BuddiesReloaded), OnBuddiesReloaded);
                WeakEventManager<BuddySettingModel, BuddyDataEventArgs>.AddHandler(_model, nameof(_model.BuddyUpdated), OnBuddyUpdated);
                OnBuddiesReloaded();
            }
        }


        private readonly BuddySettingModel _model;
        private readonly BuddySettingsSender _buddySettingsSender;

        public RProperty<bool> MainAvatarOutputActive => _model.MainAvatarOutputActive;

        private readonly ObservableCollection<BuddyItemViewModel> _items = new();
        public ReadOnlyObservableCollection<BuddyItemViewModel> Items { get; }        

        public ActionCommand OpenBuddyFolderCommand { get; }
        public ActionCommand OpenDocUrlCommand { get; }
        public ActionCommand ReloadAllCommand { get; }

        private void OnBuddiesReloaded(object? sender, EventArgs e) => OnBuddiesReloaded();
        private void OnBuddiesReloaded()
        {
            foreach(var item in _items)
            {
                item.ReloadRequested -= ReloadBuddy;
            }
            _items.Clear();

            foreach (var buddy in _model.Buddies)
            {
                var item = new BuddyItemViewModel(_buddySettingsSender, buddy);
                item.ReloadRequested += ReloadBuddy;
                _items.Add(item);
            }
        }

        private void OnBuddyUpdated(object? sender, BuddyDataEventArgs e)
        {
            _items[e.Index].ReloadRequested -= ReloadBuddy;
            _items.RemoveAt(e.Index);

            var item = new BuddyItemViewModel(_buddySettingsSender, e.BuddyData);
            item.ReloadRequested += ReloadBuddy;
            _items.Insert(e.Index, item);
        }

        internal void ReloadBuddy(BuddyData buddy) => _model.ReloadBuddy(buddy);

        private void OpenBuddyFolder()
        {
            if (Directory.Exists(SpecialFilePath.BuddyDir))
            {
                Process.Start(new ProcessStartInfo(SpecialFilePath.BuddyDir)
                {
                    UseShellExecute = true,
                });
            }
        }

        private void OpenDocUrl() => UrlNavigate.Open(LocalizedString.GetString("URL_docs_buddy"));
    }

    /// <summary> 単一のBuddyの設定に対応するVM </summary>
    public class BuddyItemViewModel
    {
        private readonly BuddyData _buddyData;

        internal BuddyItemViewModel(BuddySettingsSender settingsSender, BuddyData buddyData)
        {
            _buddyData = buddyData;
            ReloadCommand = new ActionCommand(() => ReloadRequested?.Invoke(_buddyData));
            ResetSettingsCommand = new ActionCommand(ResetSettingsAsync);
            Properties = buddyData.Properties
                .Select(p => new BuddyPropertyViewModel(settingsSender, buddyData.Metadata, p))
                .ToArray();
        }

        public event Action<BuddyData>? ReloadRequested;

        public RProperty<bool> IsActive => _buddyData.IsActive;

        public ActionCommand ReloadCommand { get; }
        public ActionCommand ResetSettingsCommand { get; }

        public IReadOnlyList<BuddyPropertyViewModel> Properties { get; }

        public string FolderName => _buddyData.Metadata.FolderName;
        public string DisplayName => _buddyData.Metadata.DisplayName;

        private async void ResetSettingsAsync()
        {
            //TODO: localize
            var result = await MessageBoxWrapper.Instance.ShowAsync(
                "サブキャラ設定のリセット",
                $"サブキャラ「{DisplayName}」の設定をデフォルト値に戻しますか？",
                MessageBoxWrapper.MessageBoxStyle.OKCancel
                )
                .ConfigureAwait(true);

            if (!result)
            {
                return;
            }

            // プロパティ変更の通知が個別でバシバシ飛ぶのが若干ダサいっちゃダサい
            // この挙動が気になる場合、明示的に設定の同期をオフにして書き換えてから一括送信…としてもよい
            foreach(var property in Properties)
            {
                property.ResetToDefault();
            }
        }
    }
}
