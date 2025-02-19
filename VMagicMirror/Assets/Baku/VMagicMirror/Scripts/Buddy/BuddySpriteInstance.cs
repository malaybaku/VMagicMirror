using System;
using Baku.VMagicMirror.Buddy.Api;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddySpriteInstance : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        private const float TransitionDuration = 0.5f;

        [SerializeField] private RawImage rawImage;
        
        public RectTransform RectTransform => (RectTransform)transform;

        private readonly Subject<Unit> _pointerEntered = new();
        public IObservable<Unit> PointerEntered => _pointerEntered;

        private readonly Subject<Unit> _pointerExit = new();
        public IObservable<Unit> PointerExit => _pointerExit;

        private readonly Subject<Unit> _pointerClicked = new();
        public IObservable<Unit> PointerClicked => _pointerClicked;

        private float _transitionTime = 0f;

        /// <summary>
        /// 指定されたトランジションを適用する。
        /// この呼び出しによってトランジションが完了した場合はtrueを返し、そうでなければfalseを返す
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="texture"></param>
        /// <param name="transitionStyle"></param>
        /// <returns></returns>
        public bool DoTransition(float deltaTime, Texture2D texture, Sprite2DTransitionStyle transitionStyle)
        {
            if (transitionStyle == Sprite2DTransitionStyle.None)
            {
                _transitionTime = 0f;
                return false;
            }

            if (transitionStyle == Sprite2DTransitionStyle.Immediate)
            {
                _transitionTime = 0f;
                rawImage.texture = texture;
                return true;
            }
            
            // TEMP:
            // - いったん常にLeftFlip
            // - Transition中に呼ばれたケースを無視
            // - ユーザーがlocalRotationを適用してるケースも無視
            _transitionTime += deltaTime;
            var rate = Mathf.Clamp01(_transitionTime / TransitionDuration);
            if (rate < 0.5f)
            {
                
                RectTransform.localRotation = Quaternion.Euler(0, 180f * rate, 0);
                return false;
            }
            else
            {
                // NOTE: 0 .. 90deg から -90degにジャンプして-90 .. 0 に進める感じ
                rawImage.texture = texture;
                RectTransform.localRotation = Quaternion.Euler(0, 180f * (rate - 1f), 0);
                if (_transitionTime >= TransitionDuration)
                {
                    _transitionTime = 0f;
                    return true;
                }
                else
                {
                    return false;
                }
            }
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

        public void SetSize(Vector2 size)
        {
            //親のCanvasに対して比率ベースで決めたい
            //正直良くわからんので一旦てきとうにやっています
            RectTransform.sizeDelta = size * 1280;
        }

        //TODO: なでなでに反応できてほしい気がするので、PointerMoveも検討したほうがいいかも
        // メインアバターに無いんかい！となるかもだが、アバターは自分だから撫でれんでも…という思想で行くとサブキャラだけ撫でられるのもアリそう
        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) 
            => _pointerEntered.OnNext(Unit.Default);
        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
            => _pointerExit.OnNext(Unit.Default);
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
            => _pointerClicked.OnNext(Unit.Default);

        public void SetParent(BuddyTransform2DInstance parent)
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
    }
}
