using System.Collections.Generic;
using UnityEngine;
using UniVRM10;

namespace Baku.VMagicMirror
{
    public class VrmaMotionSetter : PresenterBase
    {
        private const int BoneMax = (int)HumanBodyBones.LastBone;

        private bool _hasModel;
        private Vrm10Runtime _runtime;
        private Transform _hips;
        private readonly Dictionary<HumanBodyBones, Transform> _bones = new();
        private readonly Dictionary<HumanBodyBones, Quaternion> _fromCache = new();
        private readonly IVRMLoadable _vrmLoadable;
        
        public VrmaMotionSetter(IVRMLoadable vrmLoadable)
        {
            _vrmLoadable = vrmLoadable;
        }
        
        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnModelLoaded;
            _vrmLoadable.VrmDisposing += OnModelUnloaded;
        }

        private void OnModelLoaded(VrmLoadedInfo info)
        {
            _runtime = info.instance.Runtime;
            var animator = info.animator;
            for (var i = 0; i < BoneMax; i++)
            {
                var bone = (HumanBodyBones)i;
                if (bone is HumanBodyBones.Jaw or HumanBodyBones.LeftEye or HumanBodyBones.RightEye)
                {
                    continue;
                }

                if (animator.GetBoneTransform(bone) is { } t)
                {
                    _bones[bone] = t;
                    _fromCache[bone] = Quaternion.identity;
                }
            }
            _hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            _hasModel = true;
        }
   
        private void OnModelUnloaded()
        {
            _hasModel = false;
            _bones.Clear();
            _fromCache.Clear();
            _hips = null;
            _runtime = null;
        }

        /// <summary>
        /// 現在の姿勢に対し、VRMAのモーションを指定した適用率で適用する
        /// </summary>
        /// <param name="anim"></param>
        /// <param name="rate"></param>
        public void Set(VrmaInstance anim, float rate)
        {
            if (!_hasModel)
            {
                return;
            }

            if (rate <= 0f)
            {
                return;
            }

            if (rate >= 1f)
            {
                ApplyRawVrma(anim);
                return;
            }
            
            var hipPos = GetHipLocalPosition();
            CacheRotations();
            ApplyRawVrma(anim);
            SetBlendedRotations(rate);
            _hips.localPosition = hipPos;
        }
        
        /// <summary>
        /// VRMAのモーションどうしを、指定した適用率で混成して適用する。1に近いほどcurrentが優先的に適用される
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="current"></param>
        /// <param name="rate"></param>
        public void Set(VrmaInstance prev, VrmaInstance current, float rate)
        {
            if (!_hasModel)
            {
                return;
            }

            if (rate <= 0f)
            {
                ApplyRawVrma(prev);
                return;
            }

            if (rate >= 1f)
            {
                ApplyRawVrma(current);
                return;
            }

            var hipPos = GetHipLocalPosition();
            ApplyRawVrma(prev);
            CacheRotations();
            ApplyRawVrma(current);
            SetBlendedRotations(rate);
            _hips.localPosition = hipPos;
        }

        private void ApplyRawVrma(VrmaInstance instance)
        {
            Vrm10Retarget.Retarget(
                instance.Instance.ControlRig, (_runtime.ControlRig, _runtime.ControlRig)
            );
        }

        private Vector3 GetHipLocalPosition() => _bones[HumanBodyBones.Hips].localPosition;

        private void CacheRotations()
        {
            foreach (var pair in _bones)
            {
                _fromCache[pair.Key] = pair.Value.localRotation;
            }
        }

        //NOTE: 引数は0-1の範囲が前提
        // - 0.0: fromCacheの値を使う
        // - 1.0: 現在の値が優先
        private void SetBlendedRotations(float rate)
        {
            foreach (var pair in _bones)
            {
                pair.Value.localRotation = Quaternion.Slerp(
                    _fromCache[pair.Key],
                    pair.Value.localRotation,
                    rate
                );
            }
        }
    }
}
