using System;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// 実質的に <see cref="Api.Interface.ITransform2D"/> を実装しているようなクラス。
    /// Sprite2DとかGuiAreaとかの実装で使う予定
    /// </summary>
    public class BuddyTransform2DInstance : MonoBehaviour
    {
        // このオブジェクトがSetParentの対象になったときに提示するRectTransformを指定しておく。
        // Sprite2Dの場合は (Sprite/Effector/Image) の3階層が1セットになったりするので、
        // Imageの部分が登録してあればOK
        [SerializeField] private RectTransform content;
        
        private RectTransform RectTransform => (RectTransform)transform;

        public RectTransform ContentTransform => content; 

        // TODO: anchorを使う等で、普通のLocalPositionとは違う方法で指定する想定
        public Vector2 LocalPosition
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
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
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public Quaternion Rotation
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public Vector2 Pivot
        {
            get => RectTransform.pivot;
            set => RectTransform.pivot = value;
        }

        // TODO: positionの扱いはもうちょっと検討が要りそう…
        /// <summary>
        /// SpriteCanvasから見たグローバル座標にスプライトを移動させる
        /// </summary>
        /// <param name="position"></param>
        public void SetPosition(Vector2 position)
        {
            //NOTE: Parentを付け替えないでもInverseTransformPointとかでも行ける？
            var rt = RectTransform;
            var currentParent = rt.parent;

            var canvas = GetComponentInParent<BuddySpriteCanvas>();
            rt.SetParent(canvas.RectTransform);
            
            rt.anchorMin = position;
            rt.anchorMax = position;
            rt.anchoredPosition = Vector2.zero;
            
            rt.SetParent(currentParent);
        }

        public void SetParent(BuddyManifestTransform2DInstance parent)
        {
            // NOTE: SetParentした瞬間はparentにピッタリくっつく位置に移動させてるが、これでいいかは諸説ありそう
            // (そもそもPosition, Scale, Sizeの概念的な整備しないとダメかも…)
            var rt = RectTransform;
            rt.SetParent(parent.transform);
            if (parent != null)
            {
                parent.NotifyChildAdded();
                rt.localPosition = Vector3.zero;
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;
            }
        }

        public void RemoveParent()
        {
            //TODO: scaleとかがこれで無事に済むか確認しないとダメそう
            var canvas = GetComponentInParent<BuddySpriteCanvas>();
            transform.SetParent(canvas.RectTransform, false);
        }
    }
}
