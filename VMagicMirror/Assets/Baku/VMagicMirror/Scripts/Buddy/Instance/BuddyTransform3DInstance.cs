using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    // NOTE: SetParent(null)するかわりにTransform3Dぜんぶの親になるようなobjを用意して、
    // そこにぶら下げる方がヒエラルキー的には良いかも…

    /// <summary>
    /// BuddyのAPIとして生成できる3Dオブジェクトのインスタンスに付随していて、
    /// 実質的に <see cref="VMagicMirror.Buddy.ITransform3D"/> を実装するようなインスタンス
    /// </summary>
    public class BuddyTransform3DInstance : MonoBehaviour
    {
        /// <summary> 親オブジェクトの種類。そもそも親がない扱いの場合はNoneになる </summary>
        public enum ParentTypes
        {
            None,
            ManifestTransform3D,
            Transform3D,
            AvatarBone,
        }

        private readonly ReactiveProperty<HumanBodyBones> _parentBone = new(HumanBodyBones.LastBone);
        /// <summary>
        /// アバターのボーンを親にする場合、そのボーンを返す。
        /// アバターのボーンを親にしない場合は <see cref="HumanBodyBones.LastBone"/> を返す
        /// </summary>
        public IReadOnlyReactiveProperty<HumanBodyBones> ParentBone => _parentBone;

        public ParentTypes ParentType { get; private set; } = ParentTypes.None;
        
        private Transform Transform => transform;
        
        public Vector3 LocalPosition
        {
            get => transform.localPosition;
            set => transform.localPosition = value;
        }

        public Quaternion LocalRotation
        {
            get => transform.localRotation;
            set => transform.localRotation = value;
        }

        public Vector3 LocalScale
        {
            get => transform.localScale;
            set => transform.localScale = value;
        }

        public Vector3 Position
        {
            get => transform.position;
            set => transform.position = value;
        }

        public Quaternion Rotation
        {
            get => transform.rotation;
            set => transform.rotation = value;
        }

        // NOTE: このへんの関数呼び出しの前後でLocalPositionとLocalRotationの値は維持される + nullチェックが終わってることが前提
        public void SetParent(BuddyManifestTransform3DInstance parent)
        {
            ParentType = ParentTypes.ManifestTransform3D;
            _parentBone.Value = HumanBodyBones.LastBone;
            transform.SetParent(parent.Transform, false);
        }

        public void SetParent(BuddyTransform3DInstance parent)
        {
            ParentType = ParentTypes.Transform3D;
            _parentBone.Value = HumanBodyBones.LastBone;
            transform.SetParent(parent.Transform, false);
        }


        public void SetParentBone(HumanBodyBones bone)
        {
            ParentType = ParentTypes.AvatarBone;
            // NOTE: このインスタンスは自力で付け替えを行わず、ParentBoneを監視しているクラスに処理してもらう
            _parentBone.Value = bone;
        }
        
        public void RemoveParent()
        {
            ParentType = ParentTypes.None;
            transform.SetParent(null, false);
            _parentBone.Value = HumanBodyBones.LastBone;
        }

        // NOTE: この関数はAPIではなく、アバターのロード/アンロードとかを監視してるクラスから呼び出す
        public void SetParentAvatarBone(Transform avatarBone)
        {
            if (ParentType != ParentTypes.AvatarBone)
            {
                return;
            }
            
            transform.SetParent(avatarBone, false);
        }

        // NOTE: この関数はAPIではなく、アバターのロード/アンロードとかを監視してるクラスから呼び出す
        public void RemoveParentAvatarBone()
        {
            if (ParentType != ParentTypes.AvatarBone)
            {
                return;
            }

            ParentType = ParentTypes.None;
            transform.SetParent(null, false);
        }
    }
}

