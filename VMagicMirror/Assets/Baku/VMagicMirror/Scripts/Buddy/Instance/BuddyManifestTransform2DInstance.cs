using System;
using R3;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// エンドユーザーが位置決めをするようなRectTransformの一種
    /// </summary>
    /// <remarks>
    /// オブジェクトが使われないとき == Buddyがオフのときはオブジェクトが破棄されるのが期待値なため、
    /// このクラスはgameObject.SetActiveを使わないし、使うべきでもない
    /// </remarks>
    public class BuddyManifestTransform2DInstance : MonoBehaviour
    {
        [SerializeField] private BuddyManifestTransform2DGizmo gizmo;

        private readonly Subject<Unit> _layoutUpdated = new();
        /// <summary> ギズモを使ってドラッグ操作によりレイアウトを編集すると、ドラッグ操作の終了時に発火する </summary>
        public IObservable<Unit> LayoutUpdated => _layoutUpdated;

        public BuddyId BuddyId { get; set; } = BuddyId.Empty;
        public string InstanceName { get; set; } = "";
        
        private RectTransform _rt;
        private RectTransform RectTransform => _rt ??= (RectTransform)transform;
        
        public Vector2 Position
        {
            get => RectTransform.anchoredPosition;
            set => RectTransform.anchoredPosition = value;
        }

        public Vector2 Scale
        {
            get
            {
                var ls = RectTransform.localScale;
                return new Vector2(ls.x, ls.y);
            }
            // NOTE: 2Dなので厚み方向は無視
            set
            {
                // gizmoは見かけ上大きさが変わらないようにしたい & スケールがめっちゃ小さい場合は諦める
                // TODO: Vec2になったことでスケール維持の処理がちょっと怪しくなったかも…
                RectTransform.localScale = new Vector3(value.x, value.y, 1f);
                gizmo.SetScale(value.magnitude < 1e-5 ? 1 : 1 / value.magnitude);
            }
        }

        private Vector3 _rotationEuler = Vector3.zero;
        public Vector3 RotationEuler
        {
            get => _rotationEuler;
            set
            {
                _rotationEuler = value;
                RectTransform.localRotation = Quaternion.Euler(value);
            }
        }

        public Quaternion Rotation => RectTransform.localRotation;
        
        public void SetGizmoActive(bool active) => gizmo.SetActive(active);
        // NOTE: ギズモがなるべく前面に出続けるための処置
        public void NotifyChildAdded() => gizmo.SetAsLastSibling();
        // NOTE: ギズモからPosition/Rotation/Scaleを更新した場合にドラッグ終了時に呼び出す
        public void NotifyLayoutUpdated() => _layoutUpdated.OnNext(Unit.Default);
    }
}
