using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Baku.VMagicMirrorConfig
{
    class AccessorySettingSync : SettingSyncBase<AccessorySetting>
    {
        public AccessorySettingSync(IMessageSender sender, IMessageReceiver receiver) : base(sender)
        {
            receiver.ReceivedCommand += OnReceivedCommand;
        }

        /// <summary>
        /// <see cref="Items"/>の中にある指定されたアクセサリのプロパティがUnity側での操作、またはリセット操作によって更新されたときに発火します。
        /// </summary>
        public event Action<AccessoryItemSetting>? ItemUpdated;

        public AccessoryItems Items { get; set; } = new AccessoryItems();
        public string SerializedSetting { get; set; } = "";

        //NOTE: ViewModel層でItems.Itemsの中にあるアイテムをいじった場合、そのアイテムを指定して呼び出す
        //この呼び出しを行ってもSerializedSettingは更新されない
        public void UpdateItemFromUi(AccessoryItemSetting item)
        {
            //必要なアイテムのデータだけ投げつける
            var json = Serialize(item);
            SendMessage(MessageFactory.Instance.SetSingleAccessoryLayout(json));  
        }

        public override void ResetToDefault()
        {
            foreach(var item in Items.Items)
            {
                item.IsVisible = true;
                item.Position = Vector3.Zero();
                item.Rotation = Vector3.Zero();
                item.Scale = Vector3.One();
                item.Name = Path.GetFileNameWithoutExtension(item.FileName);
                ItemUpdated?.Invoke(item);
            }
            SerializedSetting = Serialize(Items);
            SendMessage(MessageFactory.Instance.SetAccessoryLayout(SerializedSetting));
            //アイテムの位置はUnity側で調整してもらう
            SendMessage(MessageFactory.Instance.RequestResetAllAccessoryLayout());
        }

        protected override void AfterLoad(AccessorySetting setting)
        {
            (Items, var missingFiles) = Deserialize(setting);
            SendMessage(MessageFactory.Instance.SetAccessoryLayout(Serialize(Items)));

            //設定ファイルになかったアイテムの情報はUnityが決めていいよ、というのをUnity側に通知する
            if (missingFiles.Length > 0)
            {
                var msg = Serialize(new AccessoryResetTargetItems()
                {
                    FileNames = missingFiles,
                });
                SendMessage(MessageFactory.Instance.RequestResetAccessoryLayout(msg));
            }
        }

        protected override void PreSave()
        {
            SerializedSetting = Serialize(Items);
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
            if (Items.Items.FirstOrDefault(i => i.FileName == item.FileName) is not AccessoryItemSetting target)
            {
                return;
            }

            target.AttachTarget = item.AttachTarget;
            target.IsVisible = item.IsVisible;
            target.Position = item.Position;
            target.Rotation = item.Rotation;
            target.Scale = item.Scale;
            ItemUpdated?.Invoke(target);
        }

        /// <summary>
        /// シリアライズされたデータのパース結果、および実行中のアプリが参照すべきフォルダに実際に入っているアイテム一覧を照合することで、
        /// 利用可能なアイテム一覧を生成します。
        /// また、シリアライズされたデータに値が入っていなかったファイルのファイル名一覧を同時に返却します。
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        private (AccessoryItems, string[]) Deserialize(AccessorySetting setting)
        {
            var result = DeserializeRaw(setting.SerializedSetting);
            var fileNames = GetAccessoryFileNames(SpecialFilePath.AccessoryFileDir);

            var missingFiles = fileNames
                .Where(n => !result.Items.Any(i => string.Compare(i.FileName, n, true) == 0))
                .ToArray();

            var items = result.Items
                .Where(i => fileNames.Contains(i.FileName))
                .ToList();

            foreach(var file in missingFiles)
            {
                items.Add(new AccessoryItemSetting() 
                {
                    FileName = file,
                    Name = Path.GetFileNameWithoutExtension(file),
                    //NOTE: ここで設定した値を、Unity側の初期値設定にあたって直ちに変更しても問題ない
                    AttachTarget = AccessoryAttachTarget.Head,
                    IsVisible = true,
                });
            }
            result.Items = items.OrderBy(i => i.FileName).ToArray();

            return (result, missingFiles);
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
            var data = new AccessoryResetTargetItems()
            {
                FileNames = new[] { fileName },
            };
            SendMessage(MessageFactory.Instance.RequestResetAccessoryLayout(Serialize(data)));
        }

        private AccessoryItemSetting DeserializeSingle(string json)
        {
            try
            {
                var serializer = new JsonSerializer();
                using (var reader = new StringReader(json))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    return serializer.Deserialize<AccessoryItemSetting>(jsonReader) ?? new AccessoryItemSetting();
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                return new AccessoryItemSetting();
            }
        }

        private string Serialize<T>(T target)
        {
            var serializer = new JsonSerializer();
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                serializer.Serialize(jsonWriter, target);
            }
            return sb.ToString();
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
