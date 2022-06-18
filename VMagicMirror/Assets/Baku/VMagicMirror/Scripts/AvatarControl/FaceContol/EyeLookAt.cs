using UnityEngine;

namespace Baku.VMagicMirror
{
    public class EyeLookAt
    {

        private float _weight = 1f;
        /// <summary>
        /// 0~1の範囲でLookAtの重みを指定する。1だと完全にターゲットを見ようとし、0だと動かない
        /// </summary>
        public float Weight
        {
            get => _weight;
            set => _weight = Mathf.Clamp01(value);
        }

        //NOTE: 右向きが正。Quaternion.Eulerの方向に従う。
        public float Yaw { get; set; }
        //NOTE: 下向きが正。Quaternion.Eulerの方向に従う。
        public float Pitch { get; set; }

        private readonly Transform _lookAtTarget;
        private Transform _head;
        private bool _hasAvatar;

        //NOTE: HeadIkIntegratorの処理を部分的に無視するために都合がよいため、いちおう見ている。見ないでも処理としては通る。
        private LookAtStyles _lookAtStyle = LookAtStyles.MousePointer;

        public EyeLookAt(IMessageReceiver receiver, IVRMLoadable vrmLoadable, Transform lookAtTarget)
        {
            _lookAtTarget = lookAtTarget;
            vrmLoadable.VrmLoaded += info => SetAvatarHead(info.animator.GetBoneTransform(HumanBodyBones.Head));
            vrmLoadable.VrmDisposing += ReleaseAvatarHead;
            
            receiver.AssignCommandHandler(
                VmmCommands.LookAtStyle,
                command => _lookAtStyle = LookAtStyleUtil.GetLookAtStyle(command.Content) 
                );
        }

        public void Calculate()
        {
            if (!_hasAvatar || _lookAtStyle == LookAtStyles.Fixed)
            {
                Yaw = 0f;
                Pitch = 0f;
                return;
            }

            var diff = _lookAtTarget.position - _head.position;
            //NOTE: めっちゃ珍しいけど一応ガード
            if (diff.magnitude < 0.01f)
            {
                Yaw = 0f;
                Pitch = 0f;
                return;
            }
            
            var localDirection = _head.InverseTransformVector(diff.normalized);
            Yaw = Weight * MathUtil.ClampedAtan2Deg(localDirection.z, localDirection.x);
            Pitch = -Weight * Mathf.Asin(localDirection.y) * Mathf.Rad2Deg;
        }
        
        private void SetAvatarHead(Transform head)
        {
            _head = head;
            _hasAvatar = true;
        }

        private void ReleaseAvatarHead()
        {
            _hasAvatar = false;
            _head = null;
            Yaw = 0f;
            Pitch = 0f;
        }
    }
}
