using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    class AccessorySettingModel : SettingModelBase<AccessorySetting>
    {
        public AccessorySettingModel() 
            : this(ModelResolver.Instance.Resolve<IMessageSender>(), ModelResolver.Instance.Resolve<IMessageReceiver>())
        {
        }

        public AccessorySettingModel(IMessageSender sender, IMessageReceiver receiver) : base(sender)
        {
            receiver.ReceivedCommand += OnReceivedCommand;
            Files = AccessoryFile.LoadAccessoryFiles(SpecialFilePath.AccessoryFileDir);
        }

        /// <summary>
        /// <see cref="Items"/>の中にある指定されたアクセサリのプロパティがUnity側での操作、またはリセット操作によって更新されたときに発火します。
        /// </summary>
        public event Action<AccessoryItemSetting>? ItemUpdated;

        /// <summary>
        /// アイテムの名前が変化したかもしれない場合に発火します。
        /// </summary>
        /// <remarks>
        /// このイベントのハンドラは表示への適用以外の処理を行ってはいけません(=ハンドラからmodel側の処理を叩くのはNG)
        /// </remarks>
        public event Action<AccessoryItemSetting>? ItemNameMaybeChanged;

        /// <summary>
        /// <see cref="RefreshFiles"/>が呼び出されてアイテムがリロードされると発火します。
        /// </summary>
        public event Action? ItemRefreshed;

        /// <summary>
        /// <see cref="ItemRefreshed"/>以外でアイテムがリロードされた可能性がある場合に発火します。
        /// </summary>
        public event Action? ItemReloaded;

        public AccessoryItems Items { get; set; } = new AccessoryItems();
        public string SerializedSetting { get; set; } = "";

        //NOTE: Filesの内容は設定ファイルに保存されないので、別クラスに分割してもOK
        public AccessoryFile[] Files { get; private set; }

        //NOTE: ViewModel層でItems.Itemsの中にあるアイテムをいじった場合、そのアイテムを指定して呼び出す
        //この呼び出しを行ってもSerializedSettingは更新されない
        public void UpdateItemFromUi(AccessoryItemSetting item)
        {
            //必要なアイテムのデータだけ投げつける
            var json = JsonConvert.SerializeObject(item);
            SendMessage(MessageFactory.SetSingleAccessoryLayout(json));  
        }

        public void NotifyItemNameMaybeChanged(AccessoryItemSetting item)
            => ItemNameMaybeChanged?.Invoke(item);

        /// <summary>
        /// アクセサリのフォルダを読み込み直すことで、明示的にアクセサリ一覧を更新します。
        /// </summary>
        public void RefreshFiles()
        {
            Files = AccessoryFile.LoadAccessoryFiles(SpecialFilePath.AccessoryFileDir);
            (Items, var missingFileIds) = FilterExistingItems(Items);
            ItemRefreshed?.Invoke();
            
            //「ファイルからの再読み込み」「レイアウト適用」「初読み込みアイテムのリセット」という
            //最大3個のメッセージを連続で送る
            SendMessage(MessageFactory.ReloadAccessoryFiles());
            SendLayoutSetup(Items, missingFileIds);
        }

        public void RequestReset(string fileId)
        {
            var files = new AccessoryResetTargetItems()
            {
                FileIds = new[] { fileId },
            };
            var json = JsonConvert.SerializeObject(files);
            SendMessage(MessageFactory.RequestResetAccessoryLayout(json));
        }

        //VMagicMirrorが初起動の場合、「再読み込み」ボタンを押したのに相当する処理を行います。
        public void RefreshIfFirstStart()
        {
            if (!File.Exists(SpecialFilePath.AutoSaveSettingFilePath))
            {
                RefreshFiles();
            }
        }

        public override void ResetToDefault()
        {
            foreach(var item in Items.Items)
            {
                item.IsVisible = true;
                item.UseBillboardMode = false;
                item.Position = Vector3.Zero();
                item.Rotation = Vector3.Zero();
                item.Scale = Vector3.One();
                item.Name = Path.GetFileNameWithoutExtension(item.FileId);
                item.ResolutionLimit = AccessoryImageResolutionLimit.None;
                item.UseAsBlinkEffect = false;
                ItemUpdated?.Invoke(item);
            }
            SerializedSetting = JsonConvert.SerializeObject(Items);
            SendMessage(MessageFactory.SetAccessoryLayout(SerializedSetting));
            //アイテムの位置はUnity側で調整してもらう
            SendMessage(MessageFactory.RequestResetAllAccessoryLayout());
        }


        protected override void AfterLoad(AccessorySetting setting)
        {
            var rawItems = DeserializeRaw(setting.SerializedSetting);
            (Items, var missingFileIds) = FilterExistingItems(rawItems);
            ItemReloaded?.Invoke();
            SendLayoutSetup(Items, missingFileIds);
        }

        protected override void PreSave()
        {
            SerializedSetting = JsonConvert.SerializeObject(Items);
        }

        private void OnReceivedCommand(CommandReceivedData e)
        {
            if (e.Command is VMagicMirror.VmmServerCommands.UpdateAccessoryLayouts)
            {
                try
                {
                    var deserialized = DeserializeRaw(e.GetStringValue());
                    foreach(var item in deserialized.Items)
                    {
                        UpdateExistingLayout(item);
                    }
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }
        }

        private void UpdateExistingLayout(AccessoryItemSetting item)
        {
            if (Items.Items.FirstOrDefault(i => i.FileId == item.FileId) is not AccessoryItemSetting target)
            {
                return;
            }

            target.AttachTarget = item.AttachTarget;
            target.IsVisible = item.IsVisible;
            target.UseBillboardMode = item.UseBillboardMode;
            target.Position = item.Position;
            target.Rotation = item.Rotation;
            target.Scale = item.Scale;
            target.ResolutionLimit = item.ResolutionLimit;
            ItemUpdated?.Invoke(target);
        }

        /// <summary>
        /// アイテム一覧とアクセサリフォルダの内訳を突き合わせて、
        /// 実際にファイルが存在するアイテムのみからなる一覧を生成します。
        /// また、保存されたデータに含まれないアクセサリのファイルID一覧を返却します。
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        private (AccessoryItems, string[]) FilterExistingItems(AccessoryItems rawItems)
        {
            var missingFileIds = Files
               .Select(f => f.FileId)
               .Where(id => !rawItems.Items.Any(i => string.Compare(i.FileId, id, true) == 0))
               .ToArray();

            var items = rawItems.Items
                .Where(i => Files.Any(f => string.Compare(i.FileId, f.FileId, true) == 0))
                .ToList();

            foreach (var fileId in missingFileIds)
            {
                items.Add(new AccessoryItemSetting()
                {
                    FileId = fileId,
                    Name = Path.GetFileNameWithoutExtension(fileId.TrimEnd(AccessoryFile.FolderIdSuffixChar)),
                    //NOTE: ここで設定した値は、Unity側の初期値設定にあたって変更しても問題ない
                    AttachTarget = AccessoryAttachTarget.Head,
                    IsVisible = true,
                });
            }
            rawItems.Items = items.OrderBy(i => i.FileId).ToArray();

            return (rawItems, missingFileIds);
        }
     
        // 設定ファイルの読み込み直後やアクセサリフォルダの再チェックを行った場合に、
        // そのチェック後のレイアウト一覧を送信するとともに、レイアウトが未定義だったファイルの初期化要求を送信します。
        private void SendLayoutSetup(AccessoryItems items, string[] missingFileIds)
        {
            var json = JsonConvert.SerializeObject(items);
            SendMessage(MessageFactory.SetAccessoryLayout(json));

            //設定ファイルになかったアイテムの情報はUnityが決めていいよ、というのをUnity側に通知する
            if (missingFileIds.Length > 0)
            {
                var files = new AccessoryResetTargetItems()
                {
                    FileIds = missingFileIds,
                };

                var msg = JsonConvert.SerializeObject(files);
                SendMessage(MessageFactory.RequestResetAccessoryLayout(msg));
            }
        }

        private static AccessoryItems DeserializeRaw(string json)
        {
            try
            {
                var serializer = new JsonSerializer();
                using (var reader = new StringReader(json))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    return serializer.Deserialize<AccessoryItems>(jsonReader) ?? new AccessoryItems();
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return new AccessoryItems();
            }
        }
    }
}
