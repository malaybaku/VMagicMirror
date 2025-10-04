using System.Collections.Generic;
using UnityEngine;
using UniVRM10;
using R3;

namespace Baku.VMagicMirror
{
    public sealed class VrmaMotionSetterLocker
    {
    }

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
            //NOTE: Hipだけゆっくり補間しても良い
            public float HipRate { get; set; }
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
            var hipPos = GetHipFixedLocalPosition();

            if (_content.UsePrevValue && _content.Rate <= 0f && _content.Rate <= 0f)
            {
                ApplyRawVrma(_content.Prev);
                ApplyHipFixedLocalPosition(hipPos);
                return;
            }

            if (_content.Rate >= 1f && _content.HipRate >= 1f)
            {
                ApplyRawVrma(_content.Current);
                ApplyHipFixedLocalPosition(hipPos);
                return;
            }

            //beforeの姿勢をキャッシュする / VRMAどうしを補間する場合だけの話
            if (_content.UsePrevValue)
            {
                ApplyRawVrma(_content.Prev);
            }
            CacheRotations();
            hipPos = _hips.localPosition;

            ApplyRawVrma(_content.Current);
            //afterの姿勢を適用してからblend
            //このとき「HipRate < 1 だが Rate == 1」というケースでRotationの書き込みをサボると効率がよい
            if (_content.Rate < 1f)
            {
                SetBlendedRotations(_content.Rate);
            }

            if (FixHipLocalPosition)
            {
                ApplyHipFixedLocalPosition(hipPos);
            }
            else
            {
                _hips.localPosition = Vector3.Lerp(hipPos, _hips.localPosition, _content.HipRate);
            }
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

        public VrmaMotionSetterLocker Locker { get; private set; }

        public bool FixHipLocalPosition { get; set; }
        
        /// <summary>
        /// MotionSetterを使うときに呼び出すことで、他クラスから処理を呼ばないようにLockerを設定する
        /// </summary>
        /// <returns></returns>
        public bool TryLock(VrmaMotionSetterLocker locker)
        {
            //NOTE: Lock済みのを再Lockするのは成功扱いにすることに注意
            if (Locker != null && Locker != locker)
            {
                return false;
            }

            Locker = locker;
            return true;
        }

        /// <summary>
        /// <see cref="TryLock"/> を呼んだクラスがこれを呼ぶことで、クラスを専有している状態を解除する
        /// </summary>
        public void ReleaseLock()
        {
            Locker = null;
        }
        
        /// <summary>
        /// 現在の姿勢に対し、VRMAのモーションを指定した適用率で適用する
        /// </summary>
        /// <param name="anim"></param>
        /// <param name="rate"></param>
        /// <param name="hipRate"></param>
        public void Set(VrmaInstance anim, float rate, float hipRate = -1f)
        {
            if (!_hasModel)
            {
                return;
            }

            _content.HasUpdate = true;
            _content.UsePrevValue = false;
            _content.Prev = null;
            _content.Current = anim;
            var smoothRate = Mathf.SmoothStep(0, 1, rate);
            _content.Rate = smoothRate;
            _content.HipRate = hipRate >= 0f ? Mathf.SmoothStep(0, 1, hipRate) : smoothRate;
        }
        
        /// <summary>
        /// VRMAのモーションどうしを、指定した適用率で混成して適用する。1に近いほどcurrentが優先的に適用される
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="anim"></param>
        /// <param name="rate"></param>
        /// <param name="hipRate"></param>
        public void Set(VrmaInstance prev, VrmaInstance anim, float rate, float hipRate = -1f)
        {
            if (!_hasModel)
            {
                return;
            }

            _content.HasUpdate = true;
            _content.UsePrevValue = rate < 1f;
            _content.Prev = prev;
            _content.Current = anim;
            var smoothRate = Mathf.SmoothStep(0, 1, rate);
            _content.Rate = smoothRate;
            _content.HipRate = hipRate >= 0f ? Mathf.SmoothStep(0, 1, hipRate) : smoothRate;
        }

        private void ApplyRawVrma(VrmaInstance instance)
        {
            Vrm10Retarget.Retarget(
                instance.Instance.ControlRig, (_runtime.ControlRig, _runtime.ControlRig)
            );
        }

        private Vector3 GetHipFixedLocalPosition() => _hips.localPosition;

        /// <summary>
        /// あらかじめGetHipFixedLocalPosition()で取得しておいた値を指定して呼び出す。
        /// Hipsの位置を固定するモードの場合だけ、実際に位置の固定処理として動作する
        /// </summary>
        /// <param name="pos"></param>
        private void ApplyHipFixedLocalPosition(Vector3 pos)
        {
            if (FixHipLocalPosition)
            {
                _hips.localPosition = pos;
            }
        }
        
        private void CacheRotations()
        {
            foreach (var pair in _bones)
            {
                _fromCache[pair.Key] = pair.Value.localRotation;
            }
        }

        //NOTE: 引数は0-1の範囲を想定しており、0や1ピッタリでは呼ばずに済む方が理想的
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
