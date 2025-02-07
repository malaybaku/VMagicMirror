using System;
using Baku.VMagicMirror.Buddy;
using UnityEngine;

namespace Baku.VMagicMirror.LuaScript
{
    /// <summary>
    /// WPFからBuddyのプロパティ情報を受けてレポジトリに保存するクラス
    /// </summary>
    public class BuddyPropertyUpdater : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly BuddyPropertyRepository _repository;

        public BuddyPropertyUpdater(IMessageReceiver receiver, BuddyPropertyRepository repository)
        {
            _receiver = receiver;
            _repository = repository;
        }

        public override void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.BuddyRefreshData,
                c => RefreshBuddyProperty(c.Content)
            );

            _receiver.AssignCommandHandler(
                VmmCommands.BuddySetProperty,
                c => SetBuddyProperty(c.Content)
            );
        }

        private void SetBuddyProperty(string json)
        {
            Debug.Log("SetBuddyProperty," + json);
            try
            {
                var msg = JsonUtility.FromJson<BuddySettingsPropertyMessage>(json);
                var api = _repository.Get(msg.BuddyId);
                api.AddOrUpdate(ConvertToProperty(msg));
            }
            catch (Exception ex)
            {
                // NOTE: コーディングエラーでのみ到達し、ユーザー起因では到達しない想定
                LogOutput.Instance.Write(ex);
            }
        }

        private void RefreshBuddyProperty(string json)
        {
            Debug.Log("RefreshBuddyProperty," + json);
            try
            {
                var msg = JsonUtility.FromJson<BuddySettingsMessage>(json);
                // リフレッシュなので、現存するプロパティをクリアする。インスタンス自体は消さないことに注意(参照が変わるとめんどいので)
                var api = _repository.Get(msg.BuddyId);
                api.Clear();
                // NOTE: property側のBuddyIdは空欄になっているはず & 無視してOK
                foreach (var property in msg.Properties)
                {
                    api.AddOrUpdate(ConvertToProperty(property));
                }
            }
            catch (Exception ex)
            {
                // NOTE: コーディングエラーでのみ到達し、ユーザー起因では到達しない想定
                LogOutput.Instance.Write(ex);
            }
        }

        // NOTE: BuddyData に対しても使う気がするぞい
        private static BuddyProperty ConvertToProperty(BuddySettingsPropertyMessage msg)
        {
            return msg.Type switch
            {
                nameof(BuddyPropertyType.Bool) => BuddyProperty.Bool(msg.Name, msg.BoolValue),
                nameof(BuddyPropertyType.Int) => BuddyProperty.Int(msg.Name, msg.IntValue),
                nameof(BuddyPropertyType.Float) => BuddyProperty.Float(msg.Name, msg.FloatValue),
                nameof(BuddyPropertyType.String) => BuddyProperty.String(msg.Name, msg.StringValue),
                nameof(BuddyPropertyType.Vector2) => BuddyProperty.Vector2(msg.Name, msg.Vector2Value.ToVector2()),
                nameof(BuddyPropertyType.Vector3) => BuddyProperty.Vector3(msg.Name, msg.Vector3Value.ToVector3()),
                nameof(BuddyPropertyType.Quaternion) => BuddyProperty.Quaternion(msg.Name, msg.Vector3Value.ToQuaternion()),
                _ => throw new NotSupportedException(),
            };
        }
    }
}
