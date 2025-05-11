using System;
using mattatz.TransformControl;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// スクリプトではなくエンドユーザーが位置決めできるような3DのTransform情報
    /// </summary>
    /// <remarks>
    /// オブジェクトが使われないとき == Buddyがオフのときはオブジェクトが破棄されるのが期待値なため、
    /// このクラスはgameObject.SetActiveを使わないし、使うべきでもない
    /// </remarks>
    public class BuddyManifestTransform3DInstance : MonoBehaviour
    {
        [SerializeField] private TransformControl transformControl;

        public Transform Transform => transform;
        
        private readonly Subject<Unit> _layoutUpdated = new();
        /// <summary> ギズモを使ってドラッグ操作によりレイアウトを編集すると、ドラッグ操作の終了時に発火する </summary>
        public IObservable<Unit> LayoutUpdated => _layoutUpdated;

        public BuddyId BuddyId { get; set; } = BuddyId.Empty;
        public string InstanceName { get; set; } = "";

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

        public Vector3 Position => transform.position;
        public Quaternion Rotation => transform.rotation;

        public Vector3 LocalScale
        {
            get => transform.localScale;
            set => transform.localScale = value;
        }

        public bool HasParentBone { get; set; }
        public HumanBodyBones ParentBone { get; set; }
        
        public void SetParent(Transform parentBone) => transform.SetParent(parentBone, false);
        public void RemoveParent() => transform.SetParent(null, false);

        public void SetTransformControlActive(bool active) => transformControl.enabled = active;

        /// <summary>
        /// フリーレイアウトが有効な間は毎フレーム呼び続ける想定
        /// </summary>
        /// <param name="request"></param>
        public void SetTransformControlRequest(TransformControlRequest request)
        {
            transformControl.global = request.WorldCoordinate;
            transformControl.mode = request.Mode;
            transformControl.Control();

            if (request.Mode != TransformControl.TransformMode.Scale)
            {
                return;
            }
            
            //スケールについては1軸だけいじったとき、残りの2軸を追従させる
            var scale = transform.localScale;
            if (Mathf.Abs(scale.x - scale.y) > Mathf.Epsilon ||
                Mathf.Abs(scale.y - scale.z) > Mathf.Epsilon ||
                Mathf.Abs(scale.z - scale.x) > Mathf.Epsilon
               )
            {
                //3つの値から1つだけ仲間はずれになっている物があるはずなので、それを探す
                var b1 = Mathf.Abs(scale.x - scale.y) > Mathf.Epsilon;
                var b2 = Mathf.Abs(scale.z - scale.x) > Mathf.Epsilon;

                var nextScale = scale.x;
                if (!b1)
                {
                    nextScale = scale.z;
                }
                else if (!b2)
                {
                    nextScale = scale.y;
                }
                //上記以外はxだけズレてる or 全軸バラバラのケースなため、x軸を使う
                transform.localScale = Vector3.one * nextScale;
            }
        }

        private void Start()
        {
            // NOTE: OnDestroyで外す…ほどでもないのでつけっぱなしにしておく
            transformControl.DragEnded += OnTransformControlDragEnded;
        }
        
        private void OnTransformControlDragEnded(TransformControl.TransformMode mode) 
            => _layoutUpdated.OnNext(Unit.Default);
    }
}
