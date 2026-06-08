using System.Collections.Generic;
using UnityEngine;
using Zenject;
using R3;

namespace Baku.VMagicMirror.FK
{
    /// <summary>
    /// Humanoidの両腕まわりのmuscleを前フレーム値と補間し、ポーズを滑らかにするクラス。
    /// ハンドトラッキング中の腕の動きのブラッシュアップ用に使う
    /// </summary>
    /// <remarks>
    /// - 37-45: left shoulder / arm / forearm / hand
    /// - 46-54: right shoulder / arm / forearm / hand
    /// </remarks>
    public sealed class ArmMuscleInterpolator : PresenterBase
    {
        private const float FilterSamplingRate = 60f;
        private const float FilterCutOffFrequency = 4f;

        // NOTE: HnadIkIntegrator.HandIkToggleDuration と同じくらいにするのが無難そうなのでそうしている
        private const float ApplyWeightChangeTime = .25f;

        private static readonly HashSet<HumanBodyBones> LeftBones = new()
        {
            HumanBodyBones.LeftShoulder,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.LeftHand,
        };

        private static readonly HashSet<HumanBodyBones> RightBones = new()
        {
            HumanBodyBones.RightShoulder,
            HumanBodyBones.RightUpperArm, 
            HumanBodyBones.RightLowerArm,
            HumanBodyBones.RightHand,
        };

        private static readonly int[] LeftArmMuscleIndices = { 37, 38, 39, 40, 41, 42, 43, 44, 45 };
        private static readonly int[] RightArmMuscleIndices = { 46, 47, 48, 49, 50, 51, 52, 53, 54 };

        private readonly IVRMLoadable _vrmLoadable;
        private readonly HandIKIntegrator _handIKIntegrator;
        private readonly CurrentFramerateChecker _framerateChecker;
        private readonly (int index, BiQuadFilter filter)[] _leftArmFilters;
        private readonly (int index, BiQuadFilter filter)[] _rightArmFilters;

        private HumanPoseHandler _humanPoseHandler;
        private Transform _hips;
        private readonly Dictionary<HumanBodyBones, Transform> _bones = new();
        private readonly Dictionary<HumanBodyBones, Quaternion> _localRotations = new();
        private HumanPose _humanPose;
        private bool _hasModel;

        private float _leftApplyWeight;
        private float _rightApplyWeight;

        [Inject]
        public ArmMuscleInterpolator(
            IVRMLoadable vrmLoadable,
            HandIKIntegrator handIKIntegrator,
            CurrentFramerateChecker framerateChecker)
        {
            _vrmLoadable = vrmLoadable;
            _handIKIntegrator = handIKIntegrator;
            _framerateChecker = framerateChecker;

            var referenceFilter = new BiQuadFilter();
            referenceFilter.SetUpAsLowPassFilter(FilterSamplingRate, FilterCutOffFrequency);
            _leftArmFilters = CreateFilters(LeftArmMuscleIndices, referenceFilter);
            _rightArmFilters = CreateFilters(RightArmMuscleIndices, referenceFilter);
        }

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;
            
            _framerateChecker.CurrentFramerate
                .Subscribe(SetupFilterFrameRate)
                .AddTo(this);
        }
        
        private void SetupFilterFrameRate(float frameRate)
        {
            var samplingRate = Mathf.Max(frameRate, 1f);
            foreach (var pair in _leftArmFilters)
            {
                pair.filter.SetUpAsLowPassFilter(samplingRate, FilterCutOffFrequency);
            }
            foreach (var pair in _rightArmFilters)
            {
                pair.filter.SetUpAsLowPassFilter(samplingRate, FilterCutOffFrequency);
            }
        }
        
        public void Update()
        {
            if (!_hasModel || _humanPoseHandler == null)
            {
                return;
            }

            _humanPoseHandler.GetHumanPose(ref _humanPose);
            var updateLeft = _handIKIntegrator.LeftTargetType.CurrentValue is HandTargetType.ImageBaseHand;
            var updateRight = _handIKIntegrator.RightTargetType.CurrentValue is HandTargetType.ImageBaseHand;

            var weightDiff = Time.deltaTime / ApplyWeightChangeTime;
            _leftApplyWeight = Mathf.MoveTowards(_leftApplyWeight, updateLeft ? 1f : 0f, weightDiff);
            _rightApplyWeight = Mathf.MoveTowards(_rightApplyWeight, updateRight ? 1f : 0f, weightDiff);

            // weight > 0 の場合、両腕どっちにもFilterが効かない場合でも
            // 「SetHumanPose由来の姿勢のずれ」を徐々に消していきたいモチベがあるので後続の処理をする
            if (!updateLeft && !updateRight && _leftApplyWeight <= 0f && _rightApplyWeight <= 0f)
            {
                ResetBothArmFilters();
                return;
            }

            // hipsは書き戻さないとズレるので明示的にキャッシュ
            var hipsLocalPosition = _hips.localPosition;
            var hipsLocalRotation = _hips.localRotation;
            
            // それ以外のboneもSetHumanPoseで微妙に動くが、動かすのはヤなので、キャッシュして書き戻す
            foreach (var (bone, t) in _bones)
            {
                _localRotations[bone] = t.localRotation;
            }

            if (updateLeft)
            {
                ApplyFilters(_leftArmFilters);
            }
            else
            {
                ResetFilters(_leftArmFilters);
            }

            if (updateRight)
            {
                ApplyFilters(_rightArmFilters);
            }
            else
            {
                ResetFilters(_rightArmFilters);
            }

            _humanPoseHandler.SetHumanPose(ref _humanPose);
            _hips.localPosition = hipsLocalPosition;
            _hips.localRotation = hipsLocalRotation;

            // 肩～手のボーンはSetHumanPoseの影響を残し、それ以外はもとに戻す
            foreach (var (bone, t) in _bones)
            {
                if (LeftBones.Contains(bone))
                {
                    // weightが高いほどSetHumanPoseの結果、つまり現在のlocalRotationを優先する
                    if (_leftApplyWeight <= 0f)
                    {
                        t.localRotation = _localRotations[bone];
                    }
                    else if (_leftApplyWeight < 1f)
                    {
                        t.localRotation = Quaternion.Slerp(_localRotations[bone], t.localRotation, _leftApplyWeight);
                    }
                }
                else if (RightBones.Contains(bone))
                {
                    if (_rightApplyWeight <= 0f)
                    {
                        t.localRotation = _localRotations[bone];
                    }
                    else if (_rightApplyWeight < 1f)
                    {
                        t.localRotation = Quaternion.Slerp(_localRotations[bone], t.localRotation, _rightApplyWeight);
                    }
                }
                else
                {
                    // 関係ないボーンの影響は全部リセット
                    t.localRotation = _localRotations[bone];
                }
            }
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            OnVrmDisposing();

            _humanPoseHandler = new HumanPoseHandler(info.Animator.avatar, info.Animator.transform);
            _hips = info.AvatarBones.Hips;
            _humanPoseHandler.GetHumanPose(ref _humanPose);
            ResetBothArmFilters();
            
            foreach (var bone in (HumanBodyBones[])System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (bone is HumanBodyBones.Hips or HumanBodyBones.Jaw or HumanBodyBones.LastBone) continue;
                var transform = info.Animator.GetBoneTransform(bone);
                if (transform != null)
                {
                    _bones[bone] = transform;
                }
            }
            
            _hasModel = true;
        }

        private void OnVrmDisposing()
        {
            _hasModel = false;
            _hips = null;
            _bones.Clear();
            _localRotations.Clear();
            _humanPoseHandler?.Dispose();
            _humanPoseHandler = null;
            _humanPose = default;
        }
        
        private void ApplyFilters((int index, BiQuadFilter filter)[] filters)
        {
            foreach (var pair in filters)
            {
                _humanPose.muscles[pair.index] = pair.filter.Update(_humanPose.muscles[pair.index]);
            }
        }

        private void ResetBothArmFilters()
        {
            ResetFilters(_leftArmFilters);
            ResetFilters(_rightArmFilters);
        }

        private void ResetFilters((int index, BiQuadFilter filter)[] filters)
        {
            foreach (var pair in filters)
            {
                pair.filter.ResetValue(_humanPose.muscles[pair.index]);
            }
        }

        private static (int index, BiQuadFilter filter)[] CreateFilters(int[] indices, BiQuadFilter referenceFilter)
        {
            var result = new (int index, BiQuadFilter filter)[indices.Length];
            for (var i = 0; i < indices.Length; i++)
            {
                var filter = new BiQuadFilter();
                filter.CopyParametersFrom(referenceFilter);
                result[i] = (indices[i], filter);
            }

            return result;
        }
    }
}
