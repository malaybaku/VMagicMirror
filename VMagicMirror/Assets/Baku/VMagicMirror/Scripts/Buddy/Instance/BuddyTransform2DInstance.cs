using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// 実質的に <see cref="Api.Interface.ITransform2D"/> を実装しているようなクラスで、
    /// スクリプトから動的生成した2Dオブジェクトの基本位置をコレで制御する
    /// </summary>
    public class BuddyTransform2DInstance : MonoBehaviour
    {
        // このオブジェクトがSetParentの対象になったときに提示するRectTransformを指定しておく。
        // Sprite2Dの場合は (Sprite/Effector/Image) の3階層が1セットになったりするので、
        // Imageの部分が登録してあればOK
        [SerializeField] private RectTransform content;
        
        private RectTransform RectTransform => (RectTransform)transform;

        private BuddySpriteCanvas _parentSpriteCanvas;
        private BuddySpriteCanvas ParentSpriteCanvas
        {
            get
            {
                if (_parentSpriteCanvas == null)
                {
                    _parentSpriteCanvas = GetComponentInParent<BuddySpriteCanvas>();
                }
                return _parentSpriteCanvas;
            }
        }

        public RectTransform ContentTransform => content != null ? content : RectTransform; 

        public Vector2 LocalPosition
        {
            get => RectTransform.anchoredPosition;
            set => RectTransform.anchoredPosition = value;
        }
  
        public Quaternion LocalRotation
        {
            get => RectTransform.localRotation;
            set => RectTransform.localRotation = value;
        }
        
        private Vector2 _localScale = Vector2.one;
        public Vector2 LocalScale
        {
            get => _localScale;
            set
            {
                _localScale = value;
                transform.localScale = new Vector3(value.x, value.y, 1f);
            }
        }
        
        public Vector2 Position
        {
            get
            {
                //TODO: 実際にはcanvasのサイズを掛ける処理とかが居るはずなので、数値を見てその辺をいじる。setterも同様
                var localPositionToCanvas =
                    ParentSpriteCanvas.RectTransform.InverseTransformPoint(transform.position);
                return localPositionToCanvas;
            }
            set
            {
                var worldPosition = ParentSpriteCanvas.RectTransform.TransformPoint(value);
                transform.position = worldPosition;
            }
        }

        public Quaternion Rotation
        {
            get => Quaternion.Inverse(ParentSpriteCanvas.RectTransform.rotation) * transform.rotation;
            set => transform.rotation = ParentSpriteCanvas.RectTransform.rotation * value;
        }

        public Vector2 Pivot
        {
            get => RectTransform.pivot;
            set => RectTransform.pivot = value;
        }
        
        public void SetParent(BuddyManifestTransform2DInstance parent)
        {
            if (parent == null)
            {
                RemoveParent();
                return;
            }

            var localPosition = LocalPosition;

            RectTransform.SetParent(parent.transform, false);
            parent.NotifyChildAdded();

            // anchoredPositionの扱いが信用できないので明示的に再更新している
            // TODO: worldPositionStays=falseでも良さげだったら下記をやらないでもいい
            LocalPosition = localPosition;
        }

        public void SetParent(BuddyTransform2DInstance parent)
        {
            if (parent == null)
            {
                RemoveParent();
                return;
            }

            var localPosition = LocalPosition;
            RectTransform.SetParent(parent.ContentTransform, false);
            // TODO: SetParentと同様、不要そうなら削除してOK
            LocalPosition = localPosition;
        }
        
        public void RemoveParent()
        {
            var localPosition = LocalPosition;
            transform.SetParent(ParentSpriteCanvas.RectTransform, false);
            // TODO: SetParentと同様、不要そうなら削除してOK
            LocalPosition = localPosition;
        }
    }
}
