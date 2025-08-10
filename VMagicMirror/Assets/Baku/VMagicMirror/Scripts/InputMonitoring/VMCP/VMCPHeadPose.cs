using R3;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.VMCP
{
    public class VMCPHeadPose : IInitializable, ITickable
    {
        private const float ResetLerpFactor = 6f;

        private readonly ReactiveProperty<bool> _isActive = new(false);
        public IReadOnlyReactiveProperty<bool> IsActive => _isActive;

        private readonly ReactiveProperty<bool> _isConnected = new(true);
        public IReadOnlyReactiveProperty<bool> IsConnected => _isConnected;

        /// <summary>
        /// NOTE: <see cref="IsActive"/>がtrueのときだけ非nullになれる
        /// </summary>
        public VMCPBasedHumanoid Humanoid { get; private set; }
        
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

        void ITickable.Tick()
        {
            //受信してるはずだけどデータが更新されない -> 直立姿勢に戻す
            if (_isActive.Value && !_isConnected.Value)
            {
                var factor = ResetLerpFactor * Time.deltaTime;
                PositionOffset = Vector3.Lerp(PositionOffset, Vector3.zero, factor);
                Rotation = Quaternion.Slerp(Rotation, Quaternion.identity, factor);
            }
        }

        private Vector3 _rawPositionOffset;
        private Quaternion _rawRotation;

        /// <summary> 直立時にゼロになるような、足元の座標系で見たときの頭の移動量 </summary>
        public Vector3 PositionOffset { get; private set; }

        /// <summary> 正面向きならゼロ回転になるような頭部回転 </summary>
        public Quaternion Rotation { get; private set; }

        public void SetActive(VMCPBasedHumanoid humanoid)
        {
            Humanoid = humanoid;
            SetActiveInternal(true);
        }

        public void SetInactive()
        {
            SetActiveInternal(false);
            Humanoid = null;
        }

        private void SetActiveInternal(bool active)
        {
            _isActive.Value = active;
            if (!active)
            {
                _isConnected.Value = false;
            }
        }

        public void SetConnected(bool connected) => _isConnected.Value = connected;

        public void SetPoseOnHips(Pose pose)
        {
            if (!_hasModel)
            {
                return;
            }

            _rawPositionOffset = pose.position - _defaultHeadOffsetOnHips;
            _rawRotation = pose.rotation;

            //NOTE: 非接続状態の場合、PositionOffsetやRotationは値をゼロに戻すので、書き込まないでおく
            if (_isActive.Value && _isConnected.Value)
            {
                PositionOffset = _rawPositionOffset;
                Rotation = _rawRotation;
            }
        }
    }
}

