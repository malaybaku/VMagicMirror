using System;
using mattatz.TransformControl;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// <see cref="BuddySpriteInstance"/>に似ているが、スクリプトではなくエンドユーザーが位置決めをするようなRectTransformの一種
    /// </summary>
    /// <remarks>
    /// オブジェクトが使われないとき == Buddyがオフのときはオブジェクトが破棄されるのが期待値なため、
    /// このクラスはgameObject.SetActiveを使わないし、使うべきでもない
    /// </remarks>
    public class BuddyTransform3DInstance : MonoBehaviour
    {
        [SerializeField] private TransformControl transformControl;

        public Transform Transform => transform;
        
        private readonly Subject<Unit> _layoutUpdated = new();
        /// <summary> ギズモを使ってドラッグ操作によりレイアウトを編集すると、ドラッグ操作の終了時に発火する </summary>
        public IObservable<Unit> LayoutUpdated => _layoutUpdated;

        public string BuddyId { get; set; } = "";
        public string InstanceName { get; set; } = "";

        public void SetGizmoActive(bool active) => transformControl.gameObject.SetActive(active);

        /// <summary> NOTE: アタッチ先がない場合はnullにしておく </summary>
        public HumanBodyBones? AttachedBone { get; set; }

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

        public float Scale
        {
            get => transform.localScale.x;
            set => transform.localScale = Vector3.one * value;
        }

        public bool HasParentBone { get; set; }
        public HumanBodyBones ParentBone { get; set; }
        
        public void SetParent(Transform parentBone) => transform.SetParent(parentBone, false);
        public void RemoveParent() => transform.SetParent(null);
    }
}
