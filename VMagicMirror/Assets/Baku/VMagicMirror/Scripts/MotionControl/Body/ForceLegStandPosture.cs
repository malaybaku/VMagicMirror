using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 下半身をロード時の姿勢のまま固定するスクリプト。
    /// 呼吸動作とFBBIKがケンカして足がクネクネするのを止めるために作成
    /// </summary>
    public class ForceLegStandPosture : MonoBehaviour
    {
        private static readonly HumanBodyBones[] _bones = new[]
        {
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.RightFoot,
            HumanBodyBones.RightToes,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.LeftToes,
        };
        
        [Inject] private IVRMLoadable _vrmLoadable = null;

        private readonly Dictionary<HumanBodyBones, Transform> _transforms
            = new Dictionary<HumanBodyBones, Transform>();
        private readonly Dictionary<HumanBodyBones, Quaternion> _initialRotations
            = new Dictionary<HumanBodyBones, Quaternion>();
        
        
        private void Start()
        {
            _vrmLoadable.VrmLoaded += info =>
            {
                _initialRotations.Clear();
                _transforms.Clear();
                var animator = info.animator;
                foreach (var bone in _bones)
                {
                    var t = animator.GetBoneTransform(bone);
                    if (t != null)
                    {
                        _transforms[bone] = t;
                        _initialRotations[bone] = t.localRotation;
                    }
                }
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _transforms.Clear();
                _initialRotations.Clear();
            };
        }

        private void LateUpdate()
        {
            foreach (var pair in _initialRotations)
            {
                _transforms[pair.Key].localRotation = pair.Value;
            }
        }
    }
}
