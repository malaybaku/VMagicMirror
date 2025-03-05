using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// BuddyのAPIとして生成できる3Dオブジェクトのインスタンスに付随していて、
    /// 実質的に <see cref="Api.Interface.ITransform3D"/> を実装するようなインスタンス
    /// </summary>
    public class BuddyTransform3DInstance : MonoBehaviour
    {
        private readonly ReactiveProperty<HumanBodyBones> _parentBone = new(HumanBodyBones.LastBone);
        /// <summary>
        /// アバターのボーンを親にする場合、そのボーンを返す。
        /// アバターのボーンを親にしない場合は <see cref="HumanBodyBones.LastBone"/> を返す
        /// </summary>
        public IReadOnlyReactiveProperty<HumanBodyBones> ParentBone => _parentBone;
        
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

        // NOTE: このへんの関数呼び出しの前後でLocalPositionとLocalRotationの値は維持される
        public void SetParent(BuddyManifestTransform3DInstance parent)
        {
            _parentBone.Value = HumanBodyBones.LastBone;
            transform.SetParent(parent.Transform, false);
        }

        public void SetParent(BuddyTransform3DInstance parent)
        {
            _parentBone.Value = HumanBodyBones.LastBone;
            transform.SetParent(parent.Transform, false);
        }


        public void SetParentBone(HumanBodyBones bone)
        {
            // NOTE: このインスタンスは自力で付け替えを行わず、ParentBoneを監視しているクラスに処理してもらう
            _parentBone.Value = bone;
        }
        
        public void RemoveParent()
        {
            transform.SetParent(null, false);
            _parentBone.Value = HumanBodyBones.LastBone;
        }

        // NOTE: この関数はAPIではなく、アバターのロード/アンロードとかを監視してるクラスから呼び出す
        public void SetParentAvatarBone(Transform avatarBone) 
            => transform.SetParent(avatarBone, false);

        // NOTE: この関数はAPIではなく、アバターのロード/アンロードとかを監視してるクラスから呼び出す
        public void RemoteParentAvatarBone() 
            => transform.SetParent(null, false);
    }
}

