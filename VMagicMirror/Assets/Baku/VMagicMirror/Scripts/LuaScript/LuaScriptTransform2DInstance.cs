using System;
using Baku.VMagicMirror.Buddy;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// <see cref="LuaScriptSpriteInstance"/>に似ているが、スクリプトではなくエンドユーザーが位置決めをするようなRectTransformの一種
    /// </summary>
    /// <remarks>
    /// オブジェクトが使われないとき == Buddyがオフのときはオブジェクトが破棄されるのが期待値なため、
    /// このクラスはgameObject.SetActiveを使わないし、使うべきでもない
    /// </remarks>
    public class LuaScriptTransform2DInstance : MonoBehaviour
    {
        [SerializeField] private Transform2DGizmo gizmo;

        private readonly Subject<Unit> _layoutUpdated = new();
        /// <summary> ギズモを使ってドラッグ操作によりレイアウトを編集すると、ドラッグ操作の終了時に発火する </summary>
        public IObservable<Unit> LayoutUpdated => _layoutUpdated;

        public string BuddyId { get; set; } = "";
        public string InstanceName { get; set; } = "";
        
        private RectTransform _rt;
        private RectTransform RectTransform
        {
            get
            {
                if (_rt == null)
                {
                    _rt = GetComponent<RectTransform>();
                }

                return _rt;
            }
        }
        
        public Vector2 Position
        {
            get => RectTransform.anchorMin;
            set
            {
                RectTransform.anchorMin = value;
                RectTransform.anchorMax = value;
            }
        }

        public float Scale
        {
            get => RectTransform.localScale.x;
            // NOTE: 2Dなので厚み方向は無視
            set
            {
                //gizmoは見かけ上大きさが変わらないようにしている
                RectTransform.localScale = new Vector3(value, value, 1);
                gizmo.SetScale(value < 1e-5 ? 1 : 1 / value);
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
