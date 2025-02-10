using System;
using System.Collections.Generic;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    public class BuddySettingModel
    {
        public BuddySettingModel() : this(
            ModelResolver.Instance.Resolve<IMessageSender>(),
            ModelResolver.Instance.Resolve<BuddySettingsSender>()
            )
        {
        }

        internal BuddySettingModel(IMessageSender sender, BuddySettingsSender buddySettingsSender)
        {
            _sender = sender;
            _buddySettingsSender = buddySettingsSender;
            MainAvatarOutputActive = new RProperty<bool>(false, v => _buddySettingsSender.SetMainAvatarOutputActive(v));
        }

        private readonly IMessageSender _sender;
        private readonly BuddySettingsSender _buddySettingsSender;

        /// <summary> 全てのBuddyを読み込み直したとき、プロパティの更新等のメッセージを送信した後で発火する </summary>
        public event EventHandler<EventArgs>? BuddiesReloaded;

        /// <summary> 特定のBuddyだけを読み込み直したとき、Buddiesの内訳が更新されてから発火する </summary>
        public event EventHandler<BuddyDataEventArgs>? BuddyUpdated;

        public RProperty<bool> MainAvatarOutputActive { get; }

        private List<BuddyData> _buddies = new();
        public IReadOnlyList<BuddyData> Buddies => _buddies;

        /// <summary>
        /// アプリケーションの起動時に一回呼ぶことで、<see cref="Buddies"/>にちゃんとしたデータが入った状態にする
        /// </summary>
        public void Load()
        {
            var saveData = BuddySaveDataRepository.LoadSetting(SpecialFilePath.BuddySettingsFilePath);
            Load(saveData);
        }

        /// <summary>
        /// 全てのBuddyの情報をリロードする
        /// </summary>
        public void ReloadAll()
        {
            // 現在メモリ上にある設定をファイルに書かれてたものとみなしてロードする。ファイルI/Oがちょっと減ってオシャレ
            var saveDatas = BuddySaveDataRepository.ExportSetting(MainAvatarOutputActive.Value, _buddies);

            foreach (var buddy in _buddies)
            {
                DisableBuddy(buddy.Metadata);
                buddy.IsActiveChanged -= OnBuddyActiveChanged;
            }

            // NOTE: Activeだったbuddyはロード処理の一環で再びアクティブになる
            Load(saveDatas);
        }

        /// <summary>
        /// 特定のBuddyだけをリロードする。このとき、リロードしたBuddyのIndexは維持されるが、インスタンスは新規に生成され直す
        /// </summary>
        /// <param name="buddy"></param>
        public void ReloadBuddy(BuddyData buddy)
        {
            var buddyIndex = _buddies.FindIndex(b => b == buddy);
            if (buddyIndex < 0)
            {
                // ないはずだが一応
                return;
            }

            if (!BuddyMetadataRepository.TryGetBuddyMetadata(buddy.Metadata.FolderPath, out var buddyMetadata))
            {
                // ここを通る場合、リロードしようにもメタデータがもうないので諦める
                // NOTE: スナックバーか何かでリロード失敗を通知できるとちょっと良さそう
                return;
            }

            var buddySaveData = BuddySaveDataRepository.ExportSetting(buddy);

            // BuddyがUnity側でアクティブだった場合、ここでDisableされる
            buddy.IsActive.Value = false;
            buddy.IsActiveChanged -= OnBuddyActiveChanged;
            _buddies.RemoveAt(buddyIndex);

            var newBuddyData = CreateBuddyData(buddyMetadata, buddySaveData);
            _buddies.Insert(buddyIndex, newBuddyData);
            _buddySettingsSender.NotifyBuddyProperties(buddy);
            if (newBuddyData.IsActive.Value)
            {
                EnableBuddy(newBuddyData.Metadata);
            }

            BuddyUpdated?.Invoke(this, new BuddyDataEventArgs(newBuddyData, buddyIndex));
        }

        /// <summary>
        /// アプリケーションの終了時に呼ぶことで、現在<see cref="Buddies"/>にある編集済みのプロパティ値を保存する
        /// </summary>
        public void Save()
            => BuddySaveDataRepository.SaveSetting(MainAvatarOutputActive.Value, _buddies, SpecialFilePath.BuddySettingsFilePath);

        public BuddyProperty? FindProperty(string buddyId, string name)
        {
            return Buddies
                .FirstOrDefault(b => b.Metadata.FolderName == buddyId)
                ?.Properties
                ?.FirstOrDefault(p => p.Metadata.Name == name);
        }

        private void Load(BuddySaveData data)
        {
            var metadatas = BuddyMetadataRepository.LoadAllBuddyMetadata();
            var buddies = new List<BuddyData>();
            foreach (var metadata in metadatas)
            {
                var savedata = data.Buddies.FirstOrDefault(b => b.Id == metadata.FolderName);
                var buddyData = CreateBuddyData(metadata, savedata);
                buddies.Add(buddyData);
            }

            _buddies = buddies;

            foreach (var buddy in _buddies)
            {
                _buddySettingsSender.NotifyBuddyProperties(buddy);

                // NOTE: Disableは別にしなくても良い (デフォルトでは非アクティブなので)
                if (buddy.IsActive.Value)
                {
                    EnableBuddy(buddy.Metadata);
                }
            }

            BuddiesReloaded?.Invoke(this, EventArgs.Empty);
            MainAvatarOutputActive.Value = data.MainAvatarOutputActive;
        }

        /// <summary>
        /// 現時点で保存してあるプロパティ(※なければnull)とメタデータからBuddyのデータを生成する。
        /// IsActiveの値は初期化され、イベント購読も行われるが、trueの場合もメッセージは呼ばないので、必要なら明示的に呼ぶこと
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="savedata"></param>
        /// <returns></returns>
        private BuddyData CreateBuddyData(BuddyMetadata metadata, BuddySaveDataSingleBuddy? savedata)
        {
            var savedProperties = savedata?.Properties ?? Array.Empty<BuddySaveDataProperty>();

            var properties = new List<BuddyProperty>();
            foreach (var propertyMetadata in metadata.Properties)
            {
                // ちゃんとした値がロードできれば使う。そうでない場合、デフォルト値に戻す
                var savedValue = savedProperties.FirstOrDefault(p => p.Name == propertyMetadata.Name)?.ToValue();
                var propertyValue = savedValue is { } && savedValue.IsValidFor(propertyMetadata)
                    ? savedValue
                    : propertyMetadata.CreateDefaultValue();
                properties.Add(new BuddyProperty(propertyMetadata, propertyValue));
            }
            var buddyData = new BuddyData(metadata, properties);
            buddyData.IsActive.Value = savedata?.IsActive ?? false;
            buddyData.IsActiveChanged += OnBuddyActiveChanged;
            return buddyData;
        }

        private void OnBuddyActiveChanged(object? sender, EventArgs e)
        {
            if (sender is not BuddyData buddy)
            {
                return;
            }

            if (buddy.IsActive.Value)
            {
                EnableBuddy(buddy.Metadata);
            }
            else
            {
                DisableBuddy(buddy.Metadata);
            }
        }

        private void EnableBuddy(BuddyMetadata buddy)
            => _sender.SendMessage(MessageFactory.Instance.BuddyEnable(buddy.FolderPath));
        
        private void DisableBuddy(BuddyMetadata buddy)
            => _sender.SendMessage(MessageFactory.Instance.BuddyDisable(buddy.FolderPath));
    }

    /// <summary> 特定のBuddyだけを再読み込みしたときに発火するイベントのデータ </summary>
    public class BuddyDataEventArgs : EventArgs
    {
        public BuddyDataEventArgs(BuddyData data, int index) : base()
        {
            BuddyData = data;
            Index = index;
        }

        public BuddyData BuddyData { get; }
        public int Index { get; }
    }
}
