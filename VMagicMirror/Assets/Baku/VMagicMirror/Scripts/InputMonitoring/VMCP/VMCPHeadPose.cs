using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.VMCP
{
    public class VMCPHeadPose : IInitializable
    {
        private readonly ReactiveProperty<bool> _isActive = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsActive => _isActive;

        //TODO: 受信が一定時間滞った場合に非接続扱いできるようにしたい
        private readonly ReactiveProperty<bool> _connected = new ReactiveProperty<bool>(true);
        public IReadOnlyReactiveProperty<bool> Connected => _connected;

        private readonly IVRMLoadable _vrmLoadable;
        //NOTE: 0にはしないでおく、一応
        private Vector3 _defaultHeadOffsetOnHips = Vector3.up;
        private bool _hasModel;

        public VMCPHeadPose(IVRMLoadable vrmLoadable)
        {
            _vrmLoadable = vrmLoadable;
        }

        public void Initialize()
        {
            _vrmLoadable.VrmLoaded += info =>
            {
                //NOTE: ロードした瞬間はyaw=0で立ってるはず
                _defaultHeadOffsetOnHips =
                    info.animator.GetBoneTransform(HumanBodyBones.Head).position -
                    info.animator.GetBoneTransform(HumanBodyBones.Hips).position;
                _hasModel = true;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _defaultHeadOffsetOnHips = Vector3.up;
            };
        }

        /// <summary> 直立時にゼロになるような、足元の座標系で見たときの頭の移動量 </summary>
        public Vector3 PositionOffset { get; private set; }

        /// <summary> 正面向きならゼロ回転になるような頭部回転 </summary>
        public Quaternion Rotation { get; private set; }

        public void SetActive(bool active) => _isActive.Value = active;

        public void SetPoseOnHips(Pose pose)
        {
            PositionOffset = pose.position - _defaultHeadOffsetOnHips;
            Rotation = pose.rotation;
        }
    }
}

