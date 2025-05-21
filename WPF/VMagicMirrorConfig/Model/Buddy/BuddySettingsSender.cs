using Baku.VMagicMirrorConfig.BuddySettingsMessages;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    // NOTE: SettingsSenderという呼称だが、ボタン押下によるアクションの通知とかも行う
    public class BuddySettingsSender
    {
        private readonly IMessageSender _sender;

        public BuddySettingsSender() : this(ModelResolver.Instance.Resolve<IMessageSender>()) { }

        internal BuddySettingsSender(IMessageSender sender)
        {
            _sender = sender;
        }

        /// <summary>
        /// あるBuddyの現在のプロパティ一式を送信する
        /// </summary>
        /// <param name="buddy"></param>
        public void NotifyBuddyProperties(BuddyData buddy)
        {
            var settings = new BuddySettingsMessage()
            {
                // NOTE: stringはカラならnullにしてしまうことにより、
                // 受信側(Unity)には空文字扱いさせつつJSONのkey:valueの書き込みを省略している
                BuddyId = buddy.Metadata.BuddyId.Value,
                Properties = buddy.Properties
                    .Where(prop => prop.Metadata.ValueType != BuddyPropertyType.Action)
                    .Select(prop => new BuddySettingsPropertyMessage()
                    {
                        Name = prop.Metadata.Name,
                        Type = prop.Metadata.ValueType.ToString(),
                        BoolValue = prop.Value.BoolValue,
                        IntValue = prop.Value.IntValue,
                        FloatValue = prop.Value.FloatValue,
                        StringValue = string.IsNullOrEmpty(prop.Value.StringValue) ? null : prop.Value.StringValue,
                        Vector2Value = prop.Value.Vector2Value,
                        Vector3Value = prop.Value.Vector3Value,
                        Transform2DValue = prop.Value.Transform2DValue,
                        Transform3DValue = prop.Value.Transform3DValue,
                    }).ToArray(),
            };
            var json = JsonConvert.SerializeObject(settings);
            _sender.SendMessage(MessageFactory.BuddyRefreshData(json));
        }

        /// <summary>
        /// あるBuddyのうち特定の1つのプロパティを送信する
        /// </summary>
        /// <param name="buddy"></param>
        /// <param name="property"></param>
        /// <param name="valueSetter"></param>
        private void NotifyProperty(
            BuddyMetadata buddy,
            BuddyPropertyMetadata property,
            Action<BuddySettingsPropertyMessage> valueSetter
            )
        {
            var msg = new BuddySettingsPropertyMessage()
            {
                BuddyId = buddy.BuddyId.Value,
                Name = property.Name,
                Type = property.ValueType.ToString(),
            };
            valueSetter(msg);
            using var sw = new StringWriter();
            new JsonSerializer().Serialize(sw, msg);
            _sender.SendMessage(MessageFactory.BuddySetProperty(sw.ToString()));
        }

        public void NotifyBoolProperty(BuddyMetadata buddy, BuddyPropertyMetadata property, bool value)
            => NotifyProperty(buddy, property, msg => msg.BoolValue = value);
        public void NotifyIntProperty(BuddyMetadata buddy, BuddyPropertyMetadata property, int value)
            => NotifyProperty(buddy, property, msg => msg.IntValue = value);
        public void NotifyFloatProperty(BuddyMetadata buddy, BuddyPropertyMetadata property, float value)
            => NotifyProperty(buddy, property, msg => msg.FloatValue = value);
        public void NotifyStringProperty(BuddyMetadata buddy, BuddyPropertyMetadata property, string value)
            => NotifyProperty(buddy, property, msg => msg.StringValue = value);
        public void NotifyVector2Property(BuddyMetadata buddy, BuddyPropertyMetadata property, BuddyVector2 value)
            => NotifyProperty(buddy, property, msg => msg.Vector2Value = value);
        public void NotifyVector3Property(BuddyMetadata buddy, BuddyPropertyMetadata property, BuddyVector3 value)
            => NotifyProperty(buddy, property, msg => msg.Vector3Value = value);
        public void NotifyTransform2DProperty(BuddyMetadata buddy, BuddyPropertyMetadata property, BuddyTransform2D value)
            => NotifyProperty(buddy, property, msg => msg.Transform2DValue = value);
        public void NotifyTransform3DProperty(BuddyMetadata buddy, BuddyPropertyMetadata property, BuddyTransform3D value)
            => NotifyProperty(buddy, property, msg => msg.Transform3DValue = value);

        public void InvokeBuddyAction(BuddyMetadata buddy, BuddyPropertyMetadata property)
        {
            var json = JsonConvert.SerializeObject(new BuddyActionMessage()
            {
                BuddyId = buddy.BuddyId.Value,
                ActionName = property.Name,
            });
            _sender.SendMessage(MessageFactory.BuddyInvokeAction(json));
        }

        public void SetMainAvatarOutputActive(bool v)
            => _sender.SendMessage(MessageFactory.BuddySetMainAvatarOutputActive(v));

        public void SetDeveloperModeActive(bool v)
            => _sender.SendMessage(MessageFactory.BuddySetDeveloperModeActive(v));

        public void SetDeveloperModeLogLevel(int v)
            => _sender.SendMessage(MessageFactory.BuddySetDeveloperModeLogLevel(v));
    }
}
