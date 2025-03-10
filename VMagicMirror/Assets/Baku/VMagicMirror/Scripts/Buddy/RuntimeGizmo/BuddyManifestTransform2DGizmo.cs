using UnityEngine;
using UnityEngine.EventSystems;

namespace Baku.VMagicMirror.Buddy
{
    //TODO: Editor上で「画面が16:9だと動くがFreeAspectだとダメ(PointerDownが期待通りに発火しない)」みたいな観察を得てるので要調査
    
    /// <summary>
    /// Transform2Dをフリーレイアウトで移動させられるすごいやつだよ
    /// </summary>
    public class BuddyManifestTransform2DGizmo :  MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private enum Transform2DDragType
        {
            None,
            TranslateX,
            TranslateY,
            TranslateXY,
            // 下記は平行移動が動いた後でやる
            Rotate,
            Scale,
        }

        // モデル
        [SerializeField] private BuddyManifestTransform2DInstance instance;
        [SerializeField] private RectTransform gizmoRoot;
        [SerializeField] private GameObject translateXGizmo;
        [SerializeField] private GameObject translateYGizmo;
        [SerializeField] private GameObject translateXYGizmo;
        // rotate/scaleは見せ方から迷い中
        [SerializeField] private GameObject rotateGizmo;
        [SerializeField] private GameObject scaleGizmo;

        private RectTransform _parentCanvasTransform;
        
        // Drag開始時に自身の位置とポインターがあった位置のズレ
        private Vector2 _offset;
        // Drag開始時点での自身の、Canvasに対する相対位置
        private Vector2 _localPositionOnPointerDown;
        // ポインターのDown/Upのあいだに一回以上OnDragが発火したかどうか
        private bool _isDragged;
        
        //TODO: フリーレイアウトモードのDragModeの選択によって決まるべきでは(=UIの操作位置には依存しないのでは)
        //↑あんま本質的じゃないと思うので一旦無視。あとでInstanceのプロパティとかを見に行くように変更するかも
        private Transform2DDragType _currentDragType;
        
        public RectTransform GizmoRoot => gizmoRoot;
        
        public void SetActive(bool active) => gameObject.SetActive(active);
        
        // NOTE: 親要素のScaleを打ち消す値を適用することでギズモのサイズを一定に保つためのAPI
        public void SetScale(float scale) => gizmoRoot.localScale = new Vector3(scale, scale, 1f);
        
        /// <summary> スプライトが子要素に加わったときに呼び出すことで、ギズモの表示が最前面になるようにする </summary>
        public void SetAsLastSibling() => gizmoRoot.SetAsLastSibling();

        private void Awake()
        {
            _parentCanvasTransform = GetComponentInParent<Canvas>().transform as RectTransform;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            var obj = eventData.pointerCurrentRaycast.gameObject;
            if (obj == translateXGizmo)
            {
                _currentDragType = Transform2DDragType.TranslateX;
            }
            else if (obj == translateYGizmo)
            {
                _currentDragType = Transform2DDragType.TranslateY;
            }
            else if (obj == translateXYGizmo)
            {
                _currentDragType = Transform2DDragType.TranslateXY;
            }
            else
            {
                //一旦無視
                return;
            }

            // Drag開始時のCanvasから見た位置は覚えとく: これは操作の種類によらず必要になる
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _parentCanvasTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var pointerLocalPoint
                ))
            {
                _currentDragType = Transform2DDragType.None;
                return;
            }

            _localPositionOnPointerDown = _parentCanvasTransform.InverseTransformPoint(gizmoRoot.position);
            _offset = _localPositionOnPointerDown - pointerLocalPoint;
            Debug.Log($"local pos on pointer down... {_localPositionOnPointerDown.x:0.0}, {_localPositionOnPointerDown.y:0.0}");
            Debug.Log($"local pos on pointer down local point.. {pointerLocalPoint.x:0.0}, {pointerLocalPoint.y:0.0}");
            _isDragged = false;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parentCanvasTransform, 
                    eventData.position,
                    eventData.pressEventCamera,
                    out var pointerLocalPoint
                ))
            {
                return;
            }

            switch (_currentDragType)
            {
                case Transform2DDragType.TranslateX:
                case Transform2DDragType.TranslateY:
                case Transform2DDragType.TranslateXY:
                    // NOTE: SpriteCanvas内では anchor = (0,0) の座標系を使うので、第3項がないと画面の半分ぶんズレてしまう
                    var destLocalPoint =
                        pointerLocalPoint +
                        _offset +
                        _parentCanvasTransform.sizeDelta * 0.5f;

                    if (_currentDragType is Transform2DDragType.TranslateX)
                    {
                        instance.Position = new Vector2(destLocalPoint.x, instance.Position.y);
                    }
                    else if (_currentDragType is Transform2DDragType.TranslateY)
                    {
                        instance.Position = new Vector2(instance.Position.x, destLocalPoint.y);
                    }
                    else
                    {
                        instance.Position = destLocalPoint;
                    }
                    break;
                default:
                    return;
            }
            _isDragged = true;
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (_currentDragType != Transform2DDragType.None && _isDragged)
            {
                instance.NotifyLayoutUpdated();
            }
            _currentDragType = Transform2DDragType.None;
            _isDragged = false;
        }
    }
}
