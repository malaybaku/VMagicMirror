using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    class AccessorySettingSync : SettingSyncBase<AccessorySetting>
    {
        public AccessorySettingSync(IMessageSender sender, IMessageReceiver receiver) : base(sender)
        {
            receiver.ReceivedCommand += OnReceivedCommand;
            Files = AccessoryFile.LoadAccessoryFiles(SpecialFilePath.AccessoryFileDir);
        }

        /// <summary>
        /// <see cref="Items"/>の中にある指定されたアクセサリのプロパティがUnity側での操作、またはリセット操作によって更新されたときに発火します。
        /// </summary>
        public event Action<AccessoryItemSetting>? ItemUpdated;

        public AccessoryItems Items { get; set; } = new AccessoryItems();
        public string SerializedSetting { get; set; } = "";

        //NOTE: 動的ロードしないのはUnity側と挙動を揃えるため。リロード可能にする場合、Unity側も対応が要る
        public AccessoryFile[] Files { get; }

        //NOTE: ViewModel層でItems.Itemsの中にあるアイテムをいじった場合、そのアイテムを指定して呼び出す
        //この呼び出しを行ってもSerializedSettingは更新されない
        public void UpdateItemFromUi(AccessoryItemSetting item)
        {
            //必要なアイテムのデータだけ投げつける
            var json = JsonConvert.SerializeObject(item);
            SendMessage(MessageFactory.Instance.SetSingleAccessoryLayout(json));  
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
                ItemUpdated?.Invoke(item);
            }
            SerializedSetting = JsonConvert.SerializeObject(Items);
            SendMessage(MessageFactory.Instance.SetAccessoryLayout(SerializedSetting));
            //アイテムの位置はUnity側で調整してもらう
            SendMessage(MessageFactory.Instance.RequestResetAllAccessoryLayout());
        }

        protected override void AfterLoad(AccessorySetting setting)
        {
            (Items, var missingFiles) = Deserialize(setting);
            var json = JsonConvert.SerializeObject(Items);
            SendMessage(MessageFactory.Instance.SetAccessoryLayout(json));

            //設定ファイルになかったアイテムの情報はUnityが決めていいよ、というのをUnity側に通知する
            if (missingFiles.Length > 0)
            {
                var files = new AccessoryResetTargetItems()
                {
                    FileIds = missingFiles,
                };

                var msg = JsonConvert.SerializeObject(files);
                SendMessage(MessageFactory.Instance.RequestResetAccessoryLayout(msg));
            }
        }

        protected override void PreSave()
        {
            SerializedSetting = JsonConvert.SerializeObject(Items);
        }

        private void OnReceivedCommand(object? sender, CommandReceivedEventArgs e)
        {
            if (e.Command == ReceiveMessageNames.UpdateAccessoryLayouts)
            {
                try
                {
                    var deserialized = DeserializeRaw(e.Args);
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
            ItemUpdated?.Invoke(target);
        }

        /// <summary>
        /// 保存されたデータとアクセサリフォルダの内容を突き合わせることで、アイテムの一覧を生成します。
        /// また、保存されたデータに含まれなかったアクセサリのファイルID一覧を返却します。
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        private (AccessoryItems, string[]) Deserialize(AccessorySetting setting)
        {
            var result = DeserializeRaw(setting.SerializedSetting);
            //var actualFiles = AccessoryFile.LoadAccessoryFiles(SpecialFilePath.AccessoryFileDir)

            var missingFileIds = Files
                .Select(f => f.FileId)
                .Where(id => !result.Items.Any(i => string.Compare(i.FileId, id, true) == 0))
                .ToArray();

            var items = result.Items
                .Where(i => Files.Any(f => string.Compare(i.FileId, f.FileId, true) == 0))
                .ToList();

            foreach(var fileId in missingFileIds)
            {
                items.Add(new AccessoryItemSetting() 
                {
                    FileId = fileId,
                    Name = Path.GetFileNameWithoutExtension(fileId),
                    //NOTE: ここで設定した値は、Unity側の初期値設定にあたって変更しても問題ない
                    AttachTarget = AccessoryAttachTarget.Head,
                    IsVisible = true,
                });
            }
            result.Items = items.OrderBy(i => i.FileId).ToArray();

            return (result, missingFileIds);
        }

        private AccessoryItems DeserializeRaw(string json)
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

        public void RequestReset(string fileName)
        {
            var files = new AccessoryResetTargetItems()
            {
                FileIds = new[] { fileName },
            };
            var json = JsonConvert.SerializeObject(files);
            SendMessage(MessageFactory.Instance.RequestResetAccessoryLayout(json));
        }

        private static string[] GetAccessoryFileNames(string dir)
        {
            return Directory.GetFiles(dir)
                .Where(f =>
                {
                    var ext = Path.GetExtension(f).ToLower();
                    return ext == ".png" || ext == ".glb" || ext == ".gltf";
                })
                .Select(f => Path.GetFileName(f))
                .ToArray();
        }
    }
}
