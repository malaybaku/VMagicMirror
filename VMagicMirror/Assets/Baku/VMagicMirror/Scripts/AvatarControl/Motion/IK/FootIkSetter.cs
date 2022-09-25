using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.IK
{
    public class FootIkSetter : PresenterBase, ITickable
    {
        private readonly IVRMLoadable _vrmLoadable;
        private readonly IKTargetTransforms _ikTarget;
        private bool _hasModel;

        private static readonly Vector3 ConstFootIkOffset = Vector3.down;
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

                //NOTE: pull = 0fを指定しているので思い切り低くしてよい
                _ikTarget.LeftFoot.position = _defaultLeftFootPosition + ConstFootIkOffset;
                _ikTarget.RightFoot.position = _defaultRightFootPosition + ConstFootIkOffset;
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
