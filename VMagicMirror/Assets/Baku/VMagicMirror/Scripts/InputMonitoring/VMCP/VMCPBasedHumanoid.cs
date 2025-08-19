using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror.VMCP
{
    /// <summary>
    /// VMCPの受信データをHumanoid扱いで何かいい感じにするために生成される、Humanoidっぽいボーン構造を持つオブジェクト
    /// 内部的にロードしたVRM + 受信したVMCPボーン情報を活用してGameObjectを生成することに注意
    /// </summary>
    public class VMCPBasedHumanoid
    {
        private const string HipsBoneName = nameof(HumanBodyBones.Hips);
        private const string HeadBoneName = nameof(HumanBodyBones.Head);

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
        private Transform _root;
        private Transform _hips;

        //下記はIK文脈で参照したいため、名指しでも取得出来るようにしとく
        private Transform _head;

        //NOTE: boneMapには常に任意ボーンも含めた全ボーンが用意される。
        // Hips/Headの位置と各ボーンのlocalRotationが主な関心になるので、任意ボーンの姿勢が初期状態相当のままになっててもあまり問題ない
        private readonly Dictionary<string, Transform> _boneMap = new(55);

        private readonly AvatarBoneInitialLocalOffsets _boneOffsets;
        private readonly bool _hasBoneOffsetsSource;

        public VMCPBasedHumanoid()
        {
            _boneOffsets = null;
            _hasBoneOffsetsSource = false;
        }
        
        public VMCPBasedHumanoid(AvatarBoneInitialLocalOffsets boneOffsets)
        {
            _boneOffsets = boneOffsets;
            _hasBoneOffsetsSource = _boneOffsets != null;
        }

        // NOTE: Root姿勢を一回も受け取ってない場合は identity が入る(= ワールド原点にアバターが立ってるアプリからのデータ送信と見なす)
        public Pose RootPose { get; private set; } = Pose.identity;

        // Hipsのローカル位置を一回以上受け取ると非null値が入る
        public Vector3? HipsLocalPosition { get; private set; }
        
        /// <summary>
        /// NOTE: この関数を呼ぶとHumanoidBoneの階層を持つGameObjectが生成される(ので、早すぎるタイミングでは呼んではいけない)
        /// </summary>
        public void GenerateHumanoidBoneHierarchy()
        {
            if (_hasBoneHierarchy)
            {
                return;
            }

            // ヒエラルキー構築はしたいが、ヒエラルキーのリファレンスになるべきモデルのロードが終わってない→何もしない
            if (_hasBoneOffsetsSource && !_boneOffsets.HasModel.CurrentValue)
            {
                return;
            }
            
            _root = new GameObject("VMCPBasedHumanoid_Root").transform;
            foreach (var bone in Enum.GetValues(typeof(HumanBodyBones)).Cast<HumanBodyBones>())
            {
                if (bone == HumanBodyBones.LastBone || 
                    bone == HumanBodyBones.LeftEye || 
                    bone == HumanBodyBones.RightEye || 
                    bone == HumanBodyBones.Jaw)
                {
                    continue;
                }

                var boneName = bone.ToString();
                _boneMap[boneName] = new GameObject(boneName).transform;
            }

            BuildBoneHierarchy();
            if (_hasBoneOffsetsSource)
            {
                ApplyLocalModelOffsets();
            }

            _hips = _boneMap[HipsBoneName];
            _head = _boneMap[HeadBoneName];
            _hasBoneHierarchy = true;
        }

        private void BuildBoneHierarchy()
        {
            foreach (var (child, parent) in BoneTree)
            {
                var childBone = _boneMap[child.ToString()];
                var parentBone = _boneMap[parent.ToString()];
                childBone.SetParent(parentBone, false);
            }

            _boneMap[HipsBoneName].SetParent(_root, false);

            foreach (var bone in _boneMap.Values)
            {
                bone.localPosition = Vector3.zero;
                bone.localRotation = Quaternion.identity;
            }
        }

        private void ApplyLocalModelOffsets()
        {
            foreach (var bone in Enum.GetValues(typeof(HumanBodyBones)).Cast<HumanBodyBones>())
            {
                //NOTE: この辺のホネは直接FKで使う予定がないのでカット
                if (bone == HumanBodyBones.LastBone ||
                    bone == HumanBodyBones.LeftEye || 
                    bone == HumanBodyBones.RightEye || 
                    bone == HumanBodyBones.Jaw)
                {
                    continue;
                }

                var boneName = bone.ToString();
                _boneMap[boneName].localPosition = _boneOffsets.GetLocalOffset(bone);
            }
        }

        public void SetRootPose(Vector3 position, Quaternion rotation)
        {
            RootPose = new Pose(position, rotation);
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

            if (!_hasBoneHierarchy)
            {
                return;
            }

            if (!_boneMap.TryGetValue(boneName, out var bone))
            {
                return;
            }

            if (boneName == HipsBoneName)
            {
                // NOTE: Hipsは通常のFKでは動かす必要がないのでビルドしたHumanoidには適用しない
                HipsLocalPosition = position;
            }
            else if (!_hasBoneOffsetsSource)
            {
                // NOTE: 受信したVMMのボーンを再構築するために値を入れている。もしVMM側のボーン情報を正とする場合、モデル側のは無視する手もある
                bone.localPosition = position;
            }
            bone.localRotation = rotation;
        }

        public Quaternion GetLocalRotation(string boneName)
        {
            return _boneMap.TryGetValue(boneName, out var bone) ? bone.localRotation : Quaternion.identity;
        }
        
        public Pose GetFKHeadPoseFromHips() => GetFKPoseOnHips(_head);

        public void Clear()
        {
            _hasBoneHierarchy = false;
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
            }

            RootPose = Pose.identity;
            HipsLocalPosition = null;
            _root = null;
            _hips = null;
            _head = null;
            _boneMap.Clear();
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
    }
}
