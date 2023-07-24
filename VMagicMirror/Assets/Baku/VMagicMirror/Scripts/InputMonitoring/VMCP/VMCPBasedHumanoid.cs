using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror.VMCP
{
    /// <summary>
    /// VMCPの受信データをHumanoid扱いで何かいい感じにするために生成される、Humanoidっぽいボーン構造を持つオブジェクト
    /// 内部的にGameObjectを生成することに注意
    /// </summary>
    public class VMCPBasedHumanoid
    {
        public static readonly string HipsBoneName = nameof(HumanBodyBones.Hips);
        public static readonly string SpineBoneName = nameof(HumanBodyBones.Spine);
        public static readonly string HeadBoneName = nameof(HumanBodyBones.Head);
        public static readonly string LeftHandBoneName = nameof(HumanBodyBones.LeftHand);
        public static readonly string RightHandBoneName = nameof(HumanBodyBones.RightHand);

        private static readonly (HumanBodyBones child, HumanBodyBones parent)[] BoneTree = new[]
        {
            (HumanBodyBones.Spine, HumanBodyBones.Hips),
            (HumanBodyBones.Chest, HumanBodyBones.Spine),
            (HumanBodyBones.UpperChest, HumanBodyBones.Chest),
            (HumanBodyBones.Neck, HumanBodyBones.UpperChest),
            (HumanBodyBones.Head, HumanBodyBones.Neck),

            (HumanBodyBones.RightShoulder, HumanBodyBones.UpperChest),
            (HumanBodyBones.RightUpperArm, HumanBodyBones.RightShoulder),
            (HumanBodyBones.RightLowerArm, HumanBodyBones.RightUpperArm),
            (HumanBodyBones.RightHand, HumanBodyBones.RightLowerArm),
            (HumanBodyBones.LeftShoulder, HumanBodyBones.UpperChest),
            (HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftShoulder),
            (HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftUpperArm),
            (HumanBodyBones.LeftHand, HumanBodyBones.LeftLowerArm),

            (HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips),
            (HumanBodyBones.RightLowerLeg, HumanBodyBones.RightUpperLeg),
            (HumanBodyBones.RightFoot, HumanBodyBones.RightLowerLeg),
            (HumanBodyBones.RightToes, HumanBodyBones.RightFoot),
            (HumanBodyBones.LeftUpperLeg, HumanBodyBones.Hips),
            (HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftUpperLeg),
            (HumanBodyBones.LeftFoot, HumanBodyBones.LeftLowerLeg),
            (HumanBodyBones.LeftToes, HumanBodyBones.LeftFoot),

            (HumanBodyBones.RightThumbProximal, HumanBodyBones.RightHand),
            (HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbProximal),
            (HumanBodyBones.RightThumbDistal, HumanBodyBones.RightThumbIntermediate),
            (HumanBodyBones.RightIndexProximal, HumanBodyBones.RightHand),
            (HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexProximal),
            (HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexIntermediate),
            (HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightHand),
            (HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleProximal),
            (HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightMiddleIntermediate),
            (HumanBodyBones.RightRingProximal, HumanBodyBones.RightHand),
            (HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingProximal),
            (HumanBodyBones.RightRingDistal, HumanBodyBones.RightRingIntermediate),
            (HumanBodyBones.RightLittleProximal, HumanBodyBones.RightHand),
            (HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleProximal),
            (HumanBodyBones.RightLittleDistal, HumanBodyBones.RightLittleIntermediate),

            (HumanBodyBones.LeftThumbProximal, HumanBodyBones.LeftHand),
            (HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbProximal),
            (HumanBodyBones.LeftThumbDistal, HumanBodyBones.LeftThumbIntermediate),
            (HumanBodyBones.LeftIndexProximal, HumanBodyBones.LeftHand),
            (HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexProximal),
            (HumanBodyBones.LeftIndexDistal, HumanBodyBones.LeftIndexIntermediate),
            (HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftHand),
            (HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleProximal),
            (HumanBodyBones.LeftMiddleDistal, HumanBodyBones.LeftMiddleIntermediate),
            (HumanBodyBones.LeftRingProximal, HumanBodyBones.LeftHand),
            (HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingProximal),
            (HumanBodyBones.LeftRingDistal, HumanBodyBones.LeftRingIntermediate),
            (HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftHand),
            (HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleProximal),
            (HumanBodyBones.LeftLittleDistal, HumanBodyBones.LeftLittleIntermediate),
        };

        private bool _hasBoneHierarchy;

        //hipsの上にもhierarchyを用意しておく(取り回しが良さそうなため)
        private Transform _root = null;
        private Transform _hips = null;

        //下記はIK文脈で参照したいため、名指しでも取得出来るようにしとく
        private Transform _head = null;
        private Transform _leftHand = null;
        private Transform _rightHand = null;

        //NOTE:
        //boneMapには常に任意ボーンも含めた全ボーンが用意される。
        //この実装方式でいいのは、VMMで最終的に使う情報がIK相当の値であるのと、基本的にはFKのlocalRotationだけ気にすればよいため
        private readonly Dictionary<string, VMCPBone> _boneMap = new Dictionary<string, VMCPBone>(55);

        //NOTE: IKポーズは単体で保存する、こっちはヒエラルキーに何かを用意する必要はない
        private readonly Dictionary<string, Pose> _trackerPoses = new Dictionary<string, Pose>(6);

        /// <summary>
        /// NOTE: この関数を呼ぶとHumanoidBoneの階層を持つGameObjectが生成される(ので、早すぎるタイミングでは呼んではいけない)
        /// </summary>
        public void GenerateHumanoidBoneHierarchy()
        {
            if (_hasBoneHierarchy)
            {
                return;
            }

            _root = new GameObject("VMCPBasedHumanoid_Root").transform;
            foreach (var bone in Enum.GetValues(typeof(HumanBodyBones)).Cast<HumanBodyBones>())
            {
                if (bone == HumanBodyBones.LastBone)
                {
                    continue;
                }

                var boneName = bone.ToString();
                _boneMap[boneName] = new VMCPBone(new GameObject(boneName).transform, bone, boneName);
            }

            BuildBoneHierarchy();

            _hips = _boneMap[HipsBoneName].Transform;
            _head = _boneMap[HeadBoneName].Transform;
            _leftHand = _boneMap[LeftHandBoneName].Transform;
            _rightHand = _boneMap[RightHandBoneName].Transform;
            _hasBoneHierarchy = true;
        }

        private void BuildBoneHierarchy()
        {
            foreach (var (child, parent) in BoneTree)
            {
                var childBone = _boneMap[child.ToString()];
                var parentBone = _boneMap[parent.ToString()];
                childBone.Transform.SetParent(parentBone.Transform, false);
            }

            _boneMap[HipsBoneName].Transform.SetParent(_root, false);

            foreach (var bone in _boneMap.Values)
            {
                bone.Transform.localPosition = Vector3.zero;
                bone.Transform.localRotation = Quaternion.identity;
            }
        }

        public void SetTrackerPose(string boneName, Vector3 position, Quaternion rotation)
        {
            _trackerPoses[boneName] = new Pose(position, rotation);
        }

        //NOTE:
        // - Hipsに対してもコレを呼び出してよい
        // - 冗長ちゃうかと思ったらpositionが無い呼び出しを定義するのを検討してもOK
        public void SetLocalPose(string boneName, Vector3 position, Quaternion rotation)
        {
            if (!_hasBoneHierarchy)
            {
                GenerateHumanoidBoneHierarchy();
            }

            if (!_boneMap.TryGetValue(boneName, out var bone))
            {
                return;
            }

            bone.Transform.localPosition = position;
            bone.Transform.localRotation = rotation;
        }

        public Pose GetFKHeadPoseFromHips() => GetFKPoseOnHips(_head);
        public Pose GetFKLeftHandPoseFromHips() => GetFKPoseOnHips(_leftHand);
        public Pose GetFKRightHandPoseFromHips() => GetFKPoseOnHips(_rightHand);

        public Pose GetIKHeadPoseOnHips() => GetIKPoseOnHips(HeadBoneName);
        public Pose GetIKLeftHandPoseOnHips() => GetIKPoseOnHips(LeftHandBoneName);
        public Pose GetIKRightHandPoseOnHips() => GetIKPoseOnHips(RightHandBoneName);

        public void Clear()
        {
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
            }

            _root = null;
            _head = null;
            _leftHand = null;
            _rightHand = null;
            _boneMap.Clear();
            _trackerPoses.Clear();
        }

        private Pose GetFKPoseOnHips(Transform bone)
        {
            if (!_hasBoneHierarchy)
            {
                return Pose.identity;
            }

            return new Pose(
                _hips.InverseTransformPoint(bone.position),
                Quaternion.Inverse(_hips.rotation) * bone.rotation
            );
        }

        private Pose GetIKPoseOnHips(string targetBoneName)
        {
            if (_trackerPoses.ContainsKey(targetBoneName) || _trackerPoses.ContainsKey(HipsBoneName))
            {
                return Pose.identity;
            }
            var hips = _trackerPoses[HipsBoneName];
            var targetBone = _trackerPoses[targetBoneName];
            return targetBone.GetTransformedBy(hips);
        }

        public Quaternion GetLocalRotation(string boneName)
        {
            if (_boneMap.TryGetValue(boneName, out var bone))
            {
                return bone.Transform.localRotation;
            }
            else
            {
                return Quaternion.identity;
            }
        }
    }

    public readonly struct VMCPBone
    {
        public Transform Transform { get; }
        public HumanBodyBones Bone { get; }
        public string BoneName { get; }
        
        public VMCPBone(Transform transform, HumanBodyBones bone, string boneName)
        {
            Transform = transform;
            Bone = bone;
            BoneName = boneName;
        }
    }
}
