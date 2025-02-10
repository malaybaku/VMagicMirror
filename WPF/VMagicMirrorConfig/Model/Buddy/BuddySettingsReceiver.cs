using Baku.VMagicMirrorConfig.BuddySettingsMessages;
using Newtonsoft.Json;
using System;
using System.IO;

namespace Baku.VMagicMirrorConfig
{
    public class BuddySettingsReceiver
    {
        public BuddySettingsReceiver() : this(
            ModelResolver.Instance.Resolve<IMessageReceiver>(),
            ModelResolver.Instance.Resolve<BuddySettingModel>()
            )
        {
        }

        internal BuddySettingsReceiver(IMessageReceiver receiver, BuddySettingModel buddySettingModel)
        {
            _receiver = receiver;
            _buddySettingModel = buddySettingModel;
            receiver.ReceivedCommand += OnReceiveCommand;
        }

        private readonly IMessageReceiver _receiver;
        private readonly BuddySettingModel _buddySettingModel;

        private void OnReceiveCommand(object? sender, CommandReceivedEventArgs e)
        {
            if (e.Command == ReceiveMessageNames.NotifyBuddy2DLayout)
            {
                try
                {
                    TrySetBuddy2DLayout(e.Args);
                }
                catch (Exception ex)
                {
                    LogOutput.Instance.Write(ex);
                }
            }
        }

        private void TrySetBuddy2DLayout(string json)
        {
            using var tr = new StringReader(json);
            using var jr = new JsonTextReader(tr);
            var serializer = new JsonSerializer();
            var message = serializer.Deserialize<BuddySettingsPropertyMessage>(jr);
            if (message == null) 
            {
                throw new InvalidOperationException("Received data is not buddy property message");
            }

            var property = _buddySettingModel.FindProperty(message.BuddyId ?? "", message.Name);
            if (property == null || property.Metadata.ValueType != BuddyPropertyType.Transform2D)
            {
                throw new InvalidOperationException("Received property is not recognized in config window");
            }

            property.Value.Transform2DValue = message.Transform2DValue;
            property.NotifyTransform2DUpdated();
        }
    }
}
