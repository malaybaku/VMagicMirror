using System;
using System.Collections.Generic;
using System.Linq;
using UniHumanoid;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// IKと競合しないようにPoseTransferするやつ
    /// </summary>
    public class LateMotionTransfer : MonoBehaviour
    {
        private float blendDuration = 0.5f;

        private float _blendRate = 0f;

        private HumanPoseTransfer _source = null;
        public HumanPoseTransfer Source
        {
            get => _source;
            set
            {
                if (_source == value)
                {
                    return;
                }
                _source = value;
                var animator = _source?.GetComponent<Animator>();

                _sourceHip = animator?.GetBoneTransform(HumanBodyBones.Hips);

                _sourceBones.Clear();
                if (animator != null)
                {
                    _sourceBones = GetHumanBodyBones()
                        .Where(b => animator.GetBoneTransform(b) != null)
                        .ToDictionary(
                            b => b,
                            b => animator.GetBoneTransform(b)
                            );
                }
            }
        }

        private HumanPoseTransfer _target = null;
        public HumanPoseTransfer Target
        {
            get => _target;
            set
            {
                if (_target == value)
                {
                    return;
                }
                _target = value;
                var animator = _target?.GetComponent<Animator>();

                _targetHip = animator?.GetBoneTransform(HumanBodyBones.Hips);

                _targetBones.Clear();
                if (animator != null)
                {
                    _targetBones = GetHumanBodyBones()
                        .Where(b => animator.GetBoneTransform(b) != null)
                        .ToDictionary(
                            b => b,
                            b => animator.GetBoneTransform(b)
                            );
                }
            }
        }

        private Transform _sourceHip = null;
        private Transform _targetHip = null;
        private HumanPose _pose;

        private Dictionary<HumanBodyBones, Transform> _sourceBones = new Dictionary<HumanBodyBones, Transform>();
        private Dictionary<HumanBodyBones, Transform> _targetBones = new Dictionary<HumanBodyBones, Transform>();

        private bool _isFadeIn = false;

        public void Fade(bool fadeIn)
        {
            _isFadeIn = fadeIn;
        }

        void LateUpdate()
        {
            float rateDiff = (1.0f / blendDuration) * Time.deltaTime;
            if (!_isFadeIn)
            {
                rateDiff = -rateDiff;
            }
            _blendRate = Mathf.Clamp01(_blendRate + rateDiff);
            

            if (Source?.PoseHandler == null ||
                Target == null ||
                Target.SourceType != HumanPoseTransfer.HumanPoseTransferSourceType.HumanPoseTransfer
                )
            {
                return;
            }

            CopyBones();

            //bool useHipTransform = (_targetHip != null);
            //Vector3 pos = useHipTransform ? _targetHip.localPosition : Vector3.zero;
            //Quaternion rot = useHipTransform ? _targetHip.localRotation : Quaternion.identity;

            //Source.PoseHandler.GetHumanPose(ref _pose);
            //Target.PoseHandler.SetHumanPose(ref _pose);

            //SetHumanPoseするとHipsから動いちゃうので戻す(※source側を合わせた方が楽しい説もあるが)
            //if (useHipTransform)
            //{
            //    _targetHip.localPosition = pos;
            //    _targetHip.localRotation = rot;
            //}
        }

        private void CopyBones()
        {
            foreach(var p in _sourceBones)
            {
                if (p.Key == HumanBodyBones.Hips) { continue; }
                if (_targetBones.ContainsKey(p.Key))
                {
                    _targetBones[p.Key].localRotation = Quaternion.Lerp(
                        _targetBones[p.Key].localRotation,
                        _sourceBones[p.Key].localRotation,
                        _blendRate
                        );
                }
            }
        }

        private static HumanBodyBones[] GetHumanBodyBones()
        {
            //HumanBodyBonesの有効値は0 ~ 54(=LastBone - 1)なので。
            var result = new HumanBodyBones[(int)HumanBodyBones.LastBone - 1];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (HumanBodyBones)i;
            }
            return result;
        }
    }
}
