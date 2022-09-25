using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.IK
{
    public class FootIkSetter : PresenterBase, ITickable
    {
        private readonly IVRMLoadable _vrmLoadable;
        private readonly IKTargetTransforms _ikTarget;
        private bool _hasModel;

        private Vector3 _defaultLeftFootPosition;
        private Vector3 _defaultRightFootPosition;
        
        public FootIkSetter(IVRMLoadable vrmLoadable, IKTargetTransforms ikTarget)
        {
            _vrmLoadable = vrmLoadable;
            _ikTarget = ikTarget;
        }

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += info =>
            {
                var leftFoot = info.animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                var rightFoot = info.animator.GetBoneTransform(HumanBodyBones.RightFoot);
                _defaultLeftFootPosition = leftFoot.position;
                _defaultRightFootPosition = rightFoot.position;

                _ikTarget.LeftFoot.position = _defaultLeftFootPosition;
                _ikTarget.RightFoot.position = _defaultRightFootPosition;
                _hasModel = true;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
            };
        }

        void ITickable.Tick()
        {
            //何もしないでもOKかも。
        }
    }
}
