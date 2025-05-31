using Baku.VMagicMirror;
using Baku.VMagicMirrorConfig.BuddySettingsMessages;
using Newtonsoft.Json;
using System;

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

        private void OnReceiveCommand(CommandReceivedData e)
        {
            try
            {
                switch (e.Command)
                {
                    case VmmServerCommands.NotifyBuddy2DLayout:
                        TrySetBuddy2DLayout(e.GetStringValue());
                        break;
                    case VmmServerCommands.NotifyBuddy3DLayout:
                        TrySetBuddy3DLayout(e.GetStringValue());
                        break;
                    case VmmServerCommands.NotifyBuddyLog:
                        SetBuddyLog(e.GetStringValue());
                        break;
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void TrySetBuddy2DLayout(string json)
        {
            var message = JsonConvert.DeserializeObject<BuddySettingsPropertyMessage>(json);
            if (message == null) 
            {
                throw new InvalidOperationException("Received data is not buddy property message");
            }

            var property = _buddySettingModel.FindProperty(new BuddyId(message.BuddyId), message.Name);
            if (property == null || property.Metadata.ValueType != BuddyPropertyType.Transform2D)
            {
                throw new InvalidOperationException("Received property is not recognized in config window");
            }

            property.Value.Transform2DValue = message.Transform2DValue;
            property.NotifyTransform2DUpdated();
        }

        private void TrySetBuddy3DLayout(string json)
        {
            var message = JsonConvert.DeserializeObject<BuddySettingsPropertyMessage>(json);
            if (message == null)
            {
                throw new InvalidOperationException("Received data is not buddy property message");
            }

            var property = _buddySettingModel.FindProperty(new BuddyId(message.BuddyId), message.Name);
            if (property == null || property.Metadata.ValueType != BuddyPropertyType.Transform3D)
            {
                throw new InvalidOperationException("Received property is not recognized in config window");
            }

            property.Value.Transform3DValue = message.Transform3DValue;
            property.NotifyTransform3DUpdated();
        }

        private void SetBuddyLog(string json)
        {
            var rawMsg = JsonConvert.DeserializeObject<RawBuddyLogMessage>(json);
            if (rawMsg == null)
            {
                return;
            }

            var msg = new BuddyLogMessage(
                new BuddyId(rawMsg.BuddyId), rawMsg.Message, rawMsg.LogLevel
                );
            _buddySettingModel.NotifyBuddyLog(msg);
        }

        // NOTE: BuddyLogMessageのほうのBuddyIdはプリミティブ型ではない (BuddyId型である) のでデシリアライズには適してない
        public record RawBuddyLogMessage(string BuddyId, string Message, int LogLevel);

    }
}
