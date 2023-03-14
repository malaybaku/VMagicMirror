using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using RootMotion.FinalIK;
using Unity.IO.LowLevel.Unsafe;
using Zenject;

namespace Baku.VMagicMirror
{
    public class TaskBasedIkWeightFader : IInitializable, ITickable, IDisposable
    {
        //ロード時点で設定されたFBBIKのウェイトを一覧で保持するやつ
        readonly struct Weights
        {
            public Weights(
                float leftShoulderPos,
                float leftHandPos,
                float leftHandRot,

                float rightShoulderPos,
                float rightHandPos,
                float rightHandRot,
                float bodyPos,

                float leftFootPos,
                float rightFootPos
                )
            {
                LeftShoulderPos = leftShoulderPos;
                LeftHandPos = leftHandPos;
                LeftHandRot = leftHandRot;

                RightShoulderPos = rightShoulderPos;
                RightHandPos = rightHandPos;
                RightHandRot = rightHandRot;

                BodyPos = bodyPos;
                LeftFootPos = leftFootPos;
                RightFootPos = rightFootPos;
            }
            
            public float LeftShoulderPos { get; }
            public float LeftHandPos { get; }
            public float LeftHandRot { get; }

            public float RightShoulderPos { get; }
            public float RightHandPos { get; }
            public float RightHandRot { get; }
            
            public float BodyPos { get; }
            
            public float LeftFootPos { get; }
            public float RightFootPos { get; }

            public static Weights Default { get; } = new Weights(
                1f, 1f, 1f, 
                1f, 1f, 1f, 
                1f, 1f, 1f
                );
        }

        [Inject]
        public TaskBasedIkWeightFader(IVRMLoadable vrmLoadable, ElbowMotionModifier elbowMotionModifier)
        {
            _vrmLoadable = vrmLoadable;
            _elbowMotionModifier = elbowMotionModifier;
        }

        private const float DefaultDuration = 0.5f;
        
        //モデルに関連する数値
        private FullBodyBipedIK _ik;
        private SimpleAnimation _simpleAnimation;
        private bool _hasModel = false;
        private bool _simpleAnimationShouldDisabled;
        private Weights _originWeight = Weights.Default;
        
        //モデルとは独立な数値
        private CancellationTokenSource _upperBodyCts;
        private CancellationTokenSource _fullBodyCts;
        
        private readonly IVRMLoadable _vrmLoadable;
        private readonly ElbowMotionModifier _elbowMotionModifier;

        //ゲーム入力によるロコモーションの適用中は0になり、それ以外は1
        private float _fullBodyIkWeight = 1f;
        //Word to Motionによるロコモーションの適用中は0になり、それ以外は1
        private float _upperBodyIkWeight = 1f;

        //何かのweightが変化したらtrueになるようなフラグ
        private bool _weightIsDirty = false;

        public void Initialize()
        {
            _vrmLoadable.PostVrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;
        }

        public void Dispose()
        {
            CancelUpperBodyWeightTask();
            CancelFullBodyWeightTask();
            _vrmLoadable.PostVrmLoaded -= OnVrmLoaded;
            _vrmLoadable.VrmDisposing -= OnVrmDisposing;
        }

        private void CancelUpperBodyWeightTask()
        {
            _upperBodyCts?.Cancel();
            _upperBodyCts?.Dispose();
            _upperBodyCts = null;
        }
        
        private void CancelFullBodyWeightTask()
        {
            _fullBodyCts?.Cancel();
            _fullBodyCts?.Dispose();
            _fullBodyCts = null;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _ik = info.fbbIk;
            var s = _ik.solver;
            _originWeight = new Weights(
                s.leftShoulderEffector.positionWeight,
                s.leftHandEffector.positionWeight,
                s.leftHandEffector.rotationWeight,
                s.rightShoulderEffector.positionWeight,
                s.rightHandEffector.positionWeight,
                s.rightHandEffector.rotationWeight,
                s.bodyEffector.positionWeight,
                s.leftFootEffector.positionWeight,
                s.rightFootEffector.positionWeight
            );
            _simpleAnimation = info.vrmRoot.GetComponent<SimpleAnimation>();
            if (_simpleAnimationShouldDisabled)
            {
                _simpleAnimation.enabled = false;
            }
            
            _hasModel = true;
            //ロード直後のweight更新をサボらないように明示的にフラグを立ててしまう
            _weightIsDirty = true;
        }

        private void OnVrmDisposing()
        {
            _hasModel = false;
            _ik = null;
            _simpleAnimation = null;
        }

        public void SetUpperBodyIkWeight(float weight, float duration = DefaultDuration)
        {
            CancelUpperBodyWeightTask();
            _upperBodyCts = new CancellationTokenSource();
            SetUpperBodyIkWeightInternalAsync(_upperBodyIkWeight, weight, duration, _upperBodyCts.Token).Forget();
        }

        public void SetFullBodyIkWeight(float weight, float duration = DefaultDuration)
        {
            CancelFullBodyWeightTask();
            _fullBodyCts = new CancellationTokenSource();
            SetFullBodyIkWeightInternalAsync(_fullBodyIkWeight, weight, duration, _fullBodyCts.Token).Forget();
        }

        //TODO: これ廃止して、BuiltInClipPlayerが普通のRuntimeAnimatorController依存になったら嬉しい…
        //NOTE: fullBodyのWeightが0になったりならなかったりする間際で呼ばれる
        void SetSimpleAnimationEnable(bool enable)
        {
            if (enable == _simpleAnimationShouldDisabled)
            {
                return;
            }

            _simpleAnimationShouldDisabled = enable;
            if (_hasModel)
            {
                _simpleAnimation.enabled = !_simpleAnimationShouldDisabled;
            }
        }

        private async UniTaskVoid SetUpperBodyIkWeightInternalAsync(
            float start, float goal, float duration, CancellationToken cancellationToken
            )
        {
            if (duration <= 0f)
            {
                //NOTE: 0とか1を指定して何度も呼んだケースをガードしている
                _weightIsDirty = _upperBodyIkWeight != goal;
                _upperBodyIkWeight = goal;
                return;
            }
            
            var count = 0f;
            while (count < duration)
            {
                var timeRate = count / duration;
                var rate = GetSmoothedRate(timeRate);
                _upperBodyIkWeight = Mathf.Lerp(start, goal, rate);
                _weightIsDirty = true;

                await UniTask.NextFrame(cancellationToken);
                count += Time.deltaTime;
            }

            _upperBodyIkWeight = goal;
            _weightIsDirty = true;
        }

        private async UniTaskVoid SetFullBodyIkWeightInternalAsync(
            float start, float goal, float duration, CancellationToken cancellationToken
        )
        {
            if (duration <= 0f)
            {
                //NOTE: 0とか1を指定して何度も呼んだケースをガードしている
                _weightIsDirty = _fullBodyIkWeight != goal;
                SetFullBodyIkWeight(goal);
                return;
            }
            
            var count = 0f;
            while (count < duration)
            {
                var timeRate = count / duration;
                var rate = GetSmoothedRate(timeRate);
                SetFullBodyIkWeight(Mathf.Lerp(start, goal, rate));
                _weightIsDirty = true;

                await UniTask.NextFrame(cancellationToken);
                count += Time.deltaTime;
            }

            SetFullBodyIkWeight(goal);
            _weightIsDirty = true;
        }

        public void Tick()
        {
            if (!_hasModel || !_weightIsDirty)
            {
                return;
            }

            var upperFactor = _upperBodyIkWeight * _fullBodyIkWeight;
            var lowerFactor = _fullBodyIkWeight;

            var s = _ik.solver;
            //上半身
            s.leftShoulderEffector.positionWeight = _originWeight.LeftShoulderPos * upperFactor;
            s.leftHandEffector.positionWeight = _originWeight.LeftHandPos * upperFactor;
            s.leftHandEffector.rotationWeight = _originWeight.LeftHandRot * upperFactor;
            s.rightShoulderEffector.positionWeight = _originWeight.RightShoulderPos * upperFactor;
            s.rightHandEffector.positionWeight = _originWeight.RightHandPos * upperFactor;
            s.rightHandEffector.rotationWeight = _originWeight.RightHandRot * upperFactor;
            _elbowMotionModifier.ElbowIkRate = upperFactor;

            //下半身
            s.bodyEffector.positionWeight = _originWeight.BodyPos * lowerFactor;
            s.leftFootEffector.positionWeight = _originWeight.LeftFootPos * lowerFactor;
            s.rightFootEffector.positionWeight = _originWeight.RightFootPos * lowerFactor;

            _weightIsDirty = false;
        }

        private void SetFullBodyIkWeight(float weight)
        {
            if (_fullBodyIkWeight <= 0 && weight > 0)
            {
                SetSimpleAnimationEnable(true);
            }
            else if (_fullBodyIkWeight > 0 && weight <= 0)
            {
                SetSimpleAnimationEnable(false);
            }
            
            _fullBodyIkWeight = weight;
        }
        
        private static float GetSmoothedRate(float t) => Mathf.SmoothStep(0f, 1f, t);
    }
}
