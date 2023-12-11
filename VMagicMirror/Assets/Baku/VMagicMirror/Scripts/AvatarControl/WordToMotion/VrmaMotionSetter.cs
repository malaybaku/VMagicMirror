using System.Collections.Generic;
using UnityEngine;
using UniVRM10;
using UniRx;

namespace Baku.VMagicMirror
{
    public class VrmaMotionSetter : PresenterBase
    {
        private const int BoneMax = (int)HumanBodyBones.LastBone;

        struct LateTickContent
        {
            public bool HasUpdate { get; set; }
            public bool UsePrevValue { get; set; }
            public VrmaInstance Prev { get; set; }
            public VrmaInstance Current { get; set; }
            public float Rate { get; set; }
        }
        
        private bool _hasModel;
        private Vrm10Runtime _runtime;
        private Transform _hips;
        private readonly Dictionary<HumanBodyBones, Transform> _bones = new();
        private readonly Dictionary<HumanBodyBones, Quaternion> _fromCache = new();
        //実行順序の関係で、LateTickの処理内容だけ特定した値をキャッシュして用いる
        private LateTickContent _content;
        
        private readonly IVRMLoadable _vrmLoadable;
        private readonly LateUpdateSourceAfterFinalIK _lateUpdateSource;
        
        public VrmaMotionSetter(
            IVRMLoadable vrmLoadable,
            LateUpdateSourceAfterFinalIK lateUpdateSource)
        {
            _vrmLoadable = vrmLoadable;
            _lateUpdateSource = lateUpdateSource;
        }
        
        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnModelLoaded;
            _vrmLoadable.VrmDisposing += OnModelUnloaded;
            _lateUpdateSource.OnLateUpdate
                .Subscribe(_ => ApplyUpdate())
                .AddTo(this);
        }

        private void ApplyUpdate()
        {
            if (!_content.HasUpdate)
            {
                return;
            }

            _content.HasUpdate = false;
            var hipPos = GetHipLocalPosition();

            if (_content.UsePrevValue && _content.Rate <= 0f)
            {
                ApplyRawVrma(_content.Prev);
                _hips.localPosition = hipPos;
                return;
            }

            if (_content.Rate >= 1f)
            {
                ApplyRawVrma(_content.Current);
                _hips.localPosition = hipPos;
                return;
            }

            //beforeの姿勢をキャッシュする / VRMAどうしを補間する場合は
            if (_content.UsePrevValue)
            {
                ApplyRawVrma(_content.Prev);
            }
            CacheRotations();

            //afterの姿勢を適用してからblend + hipsが動かないように元の位置に戻す
            ApplyRawVrma(_content.Current);
            SetBlendedRotations(_content.Rate);
            _hips.localPosition = hipPos;
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

            _content.HasUpdate = true;
            _content.UsePrevValue = false;
            _content.Prev = null;
            _content.Current = anim;
            _content.Rate = rate;
        }
        
        /// <summary>
        /// VRMAのモーションどうしを、指定した適用率で混成して適用する。1に近いほどcurrentが優先的に適用される
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="anim"></param>
        /// <param name="rate"></param>
        public void Set(VrmaInstance prev, VrmaInstance anim, float rate)
        {
            if (!_hasModel)
            {
                return;
            }

            _content.HasUpdate = true;
            _content.UsePrevValue = rate < 1f;
            _content.Prev = prev;
            _content.Current = anim;
            _content.Rate = rate;
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
