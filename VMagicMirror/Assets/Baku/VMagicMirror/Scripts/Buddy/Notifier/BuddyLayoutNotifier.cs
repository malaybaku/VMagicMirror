using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// Unity側でBuddyのレイアウト編集関連の処置をしつつ、編集が行われたらWPF側に通知するクラス
    /// </summary>
    public class BuddyLayoutEditNotifier : PresenterBase
    {
        // TODO: FreeLayoutのオン/オフは単一のRepositoryで持ちたい…
        private readonly IMessageReceiver _receiver;
        private readonly IMessageSender _sender;
        private readonly BuddyTransformInstanceRepository _repository;

        private readonly ReactiveProperty<bool> _freeLayoutActive = new();
        
        public BuddyLayoutEditNotifier(
            IMessageReceiver receiver,
            IMessageSender sender,
            BuddyTransformInstanceRepository repository)
        {
            _sender = sender;
            _receiver = receiver;
            _repository = repository;
        }
        
        public override void Initialize()
        {
            _receiver.BindBoolProperty(VmmCommands.EnableDeviceFreeLayout, _freeLayoutActive);

            //NOTE: インスタンスの破棄について、ちゃんとRemovedをチェックするようにしてもそれはそれでOK
            _repository.Added2D
                .Subscribe(instance =>
                {
                    instance.LayoutUpdated
                        .Subscribe(_ => Notify2DLayoutUpdated(instance))
                        .AddTo(instance);
                    instance.SetGizmoActive(_freeLayoutActive.Value);
                })
                .AddTo(this);

            _freeLayoutActive.Skip(1)
                .Subscribe(SetGizmoActive)
                .AddTo(this);
        }

        private void Notify2DLayoutUpdated(LuaScriptTransform2DInstance instance)
        {
            var msg = new BuddySettingsPropertyMessage()
            {
                BuddyId = instance.BuddyId,
                Name = instance.InstanceName,
                Type = nameof(BuddyPropertyType.Transform2D),
                Transform2DValue = new BuddyTransform2D()
                {
                    Position = BuddyVector2.FromVector2(instance.Position),
                    Rotation = BuddyVector3.FromVector3(instance.RotationEuler),
                    Scale = instance.Scale,
                },
            };
            
            _sender.SendCommand(MessageFactory.Instance.NotifyBuddy2DLayout(
                JsonUtility.ToJson(msg)
                ));
        }

        private void SetGizmoActive(bool active)
        {
            foreach (var instance in _repository.GetTransform2DInstances())
            {
                instance.SetGizmoActive(active);
            }
        }
    }
}
