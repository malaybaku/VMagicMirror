using System;
using System.Collections.Generic;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    public record BuddyLogMessage(BuddyId BuddyId, string Message, int LogLevel)
    {
        public BuddyLogLevel Level
        {
            get
            {
                if (LogLevel >= (int)BuddyLogLevel.Fatal && LogLevel <= (int)BuddyLogLevel.Verbose)
                {
                    return (BuddyLogLevel)LogLevel;
                }
                else
                {
                    // 未知なので重大扱いに倒しておく
                    return BuddyLogLevel.Fatal;
                }
            }
        }
    }

    public class BuddyLogMessageEventArgs : EventArgs
    {
        public BuddyLogMessageEventArgs(BuddyLogMessage message) => Message = message;
        public BuddyLogMessage Message { get; }

        public BuddyId BuddyId => Message.BuddyId;
        public string LogMessage => Message.Message;
        public BuddyLogLevel BuddyLogLevel => Message.Level;
    }

    /// <summary> 特定のBuddyだけを再読み込みしたときに発火するイベントのデータ </summary>
    public class BuddyDataEventArgs : EventArgs
    {
        public BuddyDataEventArgs(BuddyData data, int index)
        {
            BuddyData = data;
            Index = index;
        }

        public BuddyData BuddyData { get; }
        public int Index { get; }
    }

    internal class BuddySettingModel : SettingModelBase<BuddySetting>
    {
        public BuddySettingModel() : this(
            ModelResolver.Instance.Resolve<IMessageSender>(),
            ModelResolver.Instance.Resolve<BuddySettingsSender>()
            )
        {
        }

        internal BuddySettingModel(IMessageSender sender, BuddySettingsSender buddySettingsSender)
            : base(sender)
        {
            _sender = sender;
            _buddySettingsSender = buddySettingsSender;

            var defaultSetting = BuddySetting.Default;
            MainAvatarOutputActive = new RProperty<bool>(
                defaultSetting.MainAvatarOutputActive, 
                v => _buddySettingsSender.SetMainAvatarOutputActive(v));
            DeveloperModeActive = new RProperty<bool>(
                defaultSetting.DeveloperModeActive,
                v => _buddySettingsSender.SetDeveloperModeActive(v));
            DeveloperModeLogLevel = new RProperty<int>(
                defaultSetting.DeveloperModeLogLevel,
                v => _buddySettingsSender.SetDeveloperModeLogLevel(v));
        }

        private readonly IMessageSender _sender;
        private readonly BuddySettingsSender _buddySettingsSender;

        /// <summary> 全てのBuddyを読み込み直したとき、プロパティの更新等のメッセージを送信した後で発火する </summary>
        public event EventHandler<EventArgs>? BuddiesReloaded;

        /// <summary> 特定のBuddyだけを読み込み直したとき、Buddiesの内訳が更新されてから発火する </summary>
        public event EventHandler<BuddyDataEventArgs>? BuddyUpdated;

        /// <summary>
        /// Buddyのログが送られてくると発火する。
        /// NOTE: モデルはデータを保持しない == Buddy関連のビューがもし閉じるならエラー履歴は消えてもよい事にしている
        /// </summary>
        public event EventHandler<BuddyLogMessageEventArgs>? ReceivedLog;

        public RProperty<bool> DeveloperModeActive { get; }
        public RProperty<bool> MainAvatarOutputActive { get; }
        public RProperty<int> DeveloperModeLogLevel { get; }

        // NOTE: 開発者モードが無効な場合、セーブしてある設定は無視する
        public BuddyLogLevel CurrentLogLevel => DeveloperModeActive.Value
            ? (BuddyLogLevel)DeveloperModeLogLevel.Value
            : BuddyLogLevel.Fatal;

        private List<BuddyData> _buddies = [];
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
                DisableBuddy(buddy);
                buddy.IsActiveChanged -= OnBuddyActiveChanged;
            }

            // NOTE: Activeだったbuddyはロード処理の一環で再びアクティブになり、イベントハンドラも再登録される
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

            if (!BuddyMetadataRepository.TryGetBuddyMetadata(buddy.Metadata.FolderPath, buddy.Metadata.IsDefaultBuddy, out var buddyMetadata))
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

            // イベントハンドラはCreateBuddyDataの過程で再登録される
            var newBuddyData = CreateBuddyData(buddyMetadata, buddySaveData);
            _buddies.Insert(buddyIndex, newBuddyData);
            _buddySettingsSender.NotifyBuddyProperties(buddy);
            if (newBuddyData.IsActive.Value)
            {
                EnableBuddy(newBuddyData);
            }

            BuddyUpdated?.Invoke(this, new BuddyDataEventArgs(newBuddyData, buddyIndex));
        }

        /// <summary>
        /// アプリケーションの終了時に呼ぶことで、現在<see cref="Buddies"/>にある編集済みのプロパティ値を保存する
        /// </summary>
        public void SaveBuddySettings()
            => BuddySaveDataRepository.SaveSetting(MainAvatarOutputActive.Value, _buddies, SpecialFilePath.BuddySettingsFilePath);

        public BuddyProperty? FindProperty(BuddyId buddyId, string name)
        {
            return Buddies
                .FirstOrDefault(b => b.Metadata.BuddyId.Equals(buddyId))
                ?.Properties
                ?.FirstOrDefault(p => p.Metadata.Name == name);
        }

        public void NotifyBuddyLog(BuddyLogMessage log)
            => ReceivedLog?.Invoke(this, new BuddyLogMessageEventArgs(log));

        public override void ResetToDefault()
        {
            // TODO: Buddyのプロパティのリセットはしない、理由は2つ
            // - 内部挙動として、各Buddyの設定はメインの設定ファイルとは別である
            // - UIとして、Buddyのプロパティは各Buddyごとにリセットするほうが望ましいはず

            var defaultSetting = BuddySetting.Default;
            MainAvatarOutputActive.Value = defaultSetting.MainAvatarOutputActive;
            DeveloperModeActive.Value = defaultSetting.DeveloperModeActive;
            DeveloperModeLogLevel.Value = defaultSetting.DeveloperModeLogLevel;
        }

        private void Load(BuddySaveData data)
        {
            var metadatas = BuddyMetadataRepository.LoadAllBuddyMetadata();
            var buddies = new List<BuddyData>();
            foreach (var metadata in metadatas)
            {
                var savedata = data.Buddies.FirstOrDefault(b => b.Id.Equals(metadata.BuddyId));
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
                    EnableBuddy(buddy);
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
                EnableBuddy(buddy);
            }
            else
            {
                DisableBuddy(buddy);
            }
        }

        private void EnableBuddy(BuddyData buddy)
        {
            _sender.SendMessage(MessageFactory.BuddyEnable(buddy.Metadata.FolderPath));
            if (!DeveloperModeActive.Value)
            {
                buddy.IsEnabledWithoutDeveloperMode.Value = true;
            }
        }

        private void DisableBuddy(BuddyData buddy)
        {
            _sender.SendMessage(MessageFactory.BuddyDisable(buddy.Metadata.FolderPath));
            buddy.IsEnabledWithoutDeveloperMode.Value = false;
        }
    }
}
