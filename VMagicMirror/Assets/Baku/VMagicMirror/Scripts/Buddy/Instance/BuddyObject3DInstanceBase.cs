using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// BuddyのAPIとして生成できる3Dオブジェクトのインスタンスの共通実装
    /// </summary>
    public abstract class BuddyObject3DInstanceBase : MonoBehaviour
    {
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

        public Vector3 GetWorldPosition() => transform.position;
        public Quaternion GetWorldRotation() => transform.rotation;
        public void SetWorldPosition(Vector3 position) => transform.position = position;
        public void SetWorldRotation(Quaternion rotation) => transform.rotation = rotation;

        /// <summary>
        /// 親になるTransform3Dを指定する。
        /// この関数の呼び出し前後でLocalPositionとLocalRotationの値は維持される
        /// </summary>
        /// <param name="parent"></param>
        public void SetParent(BuddyTransform3DInstance parent) => transform.SetParent(parent.Transform, false);
    }
}

