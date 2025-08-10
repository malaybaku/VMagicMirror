using UnityEngine;
using R3;

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
        private readonly BuddyLayoutRepository _layoutRepository;
        private readonly BuddyManifestTransformInstanceRepository _repository;

        private readonly ReactiveProperty<bool> _freeLayoutActive = new();
        
        public BuddyLayoutEditNotifier(
            IMessageReceiver receiver,
            IMessageSender sender,
            BuddyLayoutRepository layoutRepository,
            BuddyManifestTransformInstanceRepository repository)
        {
            _sender = sender;
            _receiver = receiver;
            _layoutRepository = layoutRepository;
            _repository = repository;
        }
        
        public override void Initialize()
        {
            _receiver.BindBoolProperty(VmmCommands.EnableDeviceFreeLayout, _freeLayoutActive);

            //NOTE: インスタンスの破棄について、ちゃんとRemovedをチェックするようにしてもそれはそれでOK。ここではAddToによって生存期間を管理している
            _repository.Transform2DAdded
                .Subscribe(instance =>
                {
                    instance.LayoutUpdated
                        .Subscribe(_ => Notify2DLayoutUpdated(instance))
                        .AddTo(instance);
                    instance.SetGizmoActive(_freeLayoutActive.Value);
                })
                .AddTo(this);

            _repository.Transform3DAdded
                .Subscribe(instance =>
                {
                    instance.LayoutUpdated
                        .Subscribe(_ => Notify3DLayoutUpdated(instance))
                        .AddTo(instance);
                    instance.SetTransformControlActive(_freeLayoutActive.Value);
                })
                .AddTo(this);
            
            _freeLayoutActive.Skip(1)
                .Subscribe(SetGizmoActive)
                .AddTo(this);
        }

        private void Notify2DLayoutUpdated(BuddyManifestTransform2DInstance instance)
        {
            // NOTE: WPFからデータの送り返しが発生しない前提で、Unity側のレイアウト情報も上書きしておく。3Dも同様
            _layoutRepository.Get(instance.BuddyId).AddOrUpdate(
                instance.InstanceName,
                new BuddyTransform2DLayout(
                    instance.Position,
                    instance.RotationEuler,
                    instance.Scale.x
                    )
                );
            
            var msg = new BuddySettingsPropertyMessage()
            {
                BuddyId = instance.BuddyId.Value,
                Name = instance.InstanceName,
                Type = nameof(BuddyPropertyType.Transform2D),
                Transform2DValue = new BuddyTransform2D()
                {
                    Position = BuddyVector2.FromVector2(instance.Position),
                    Rotation = BuddyVector3.FromVector3(instance.RotationEuler),
                    Scale = instance.Scale.x,
                },
            };
            
            _sender.SendCommand(MessageFactory.NotifyBuddy2DLayout(
                JsonUtility.ToJson(msg)
                ));
        }
        
        private void Notify3DLayoutUpdated(BuddyManifestTransform3DInstance instance)
        {
            // NOTE: BuddyLayoutUpdaterの受信処理では「ParentBoneが変化したら云々」みたいな処理が入っているので複雑だが、
            // この関数を通過するケースではParentBoneは変化しないので、単に値を入れておくだけでOK
            _layoutRepository.Get(instance.BuddyId).AddOrUpdate(
                instance.InstanceName,
                new BuddyTransform3DLayout(
                    instance.LocalPosition,
                    instance.LocalRotation,
                    instance.LocalScale.x,
                    instance.HasParentBone ? instance.ParentBone : null
                )
            );

            // NOTE: フリーレイアウトで編集しうるのはPos/Rot/Scaleの3つだけで、ParentBoneは編集はされない想定
            var msg = new BuddySettingsPropertyMessage()
            {
                BuddyId = instance.BuddyId.Value,
                Name = instance.InstanceName,
                Type = nameof(BuddyPropertyType.Transform3D),
                Transform3DValue = new BuddyTransform3D()
                {
                    Position = BuddyVector3.FromVector3(instance.LocalPosition),
                    Rotation = BuddyVector3.FromVector3(instance.LocalRotation.eulerAngles),
                    Scale = instance.LocalScale.x,
                    ParentBone = instance.HasParentBone ? (int) instance.ParentBone : -1
                },
            };
            
            _sender.SendCommand(MessageFactory.NotifyBuddy3DLayout(
                JsonUtility.ToJson(msg)
            ));
        }

        private void SetGizmoActive(bool active)
        {
            foreach (var instance in _repository.GetTransform2DInstances())
            {
                instance.SetGizmoActive(active);
            }

            foreach (var instance in _repository.GetTransform3DInstances())
            {
                instance.SetTransformControlActive(active);
            }
        }
    }
}
