using System;
using System.Collections.Generic;
using Baku.VMagicMirror.Buddy.Api;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Baku.VMagicMirror.Buddy
{
    public class BuddySpriteInstance : MonoBehaviour, 
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerMoveHandler
    {
        private const float TransitionDuration = 0.5f;

        [SerializeField] private RectTransform effectorRectTransform;
        [SerializeField] private RawImage rawImage;
        
        // NOTE: CurrentTransitionStyleがNone以外な場合、このテクスチャが実際に表示されているとは限らない
        public Texture2D CurrentTexture { get; private set; }

        // NOTE: この値はトランジション処理をやっているクラスがトランジションを完了すると、自動でNoneに切り替わる
        public Sprite2DTransitionStyle CurrentTransitionStyle { get; set; } = Sprite2DTransitionStyle.None;

        private readonly Dictionary<string, Texture2D> _textures = new();
        
        // TODO: 公開しない方向で。
        public RectTransform RectTransform => (RectTransform)transform;

        /// <summary> バウンド効果などのエフェクトを適用可能なRectTransformを取得する </summary>
        public RectTransform EffectorRectTransform => effectorRectTransform;
        
        private readonly Subject<Unit> _pointerEntered = new();
        public IObservable<Unit> PointerEntered => _pointerEntered;

        private readonly Subject<Unit> _pointerExit = new();
        public IObservable<Unit> PointerExit => _pointerExit;

        private readonly Subject<Unit> _pointerClicked = new();
        public IObservable<Unit> PointerClicked => _pointerClicked;

        // TODO: anchorを使う等で、普通のLocalPositionとは違う方法で指定する
        public Vector2 LocalPosition
        {
            get;
            set;
        }
        // NOTE: Vector3に変更するかも
        public Quaternion LocalRotation
        {
            get => RectTransform.localRotation;
            set => RectTransform.localRotation = value;
        }
        
        // TODO: デフォが1になるようなキメにできたほうが嬉しい
        private Vector2 _size = new(0.1f, 0.1f);
        public Vector2 Size
        {
            get => _size;
            set
            {
                _size = value;
                //親のCanvasに対して比率ベースで決めたい
                //正直良くわからんので一旦てきとうにやっています
                RectTransform.sizeDelta = value * 1280;
            }
        }

        public Vector2 Pivot
        {
            get => RectTransform.pivot;
            set => RectTransform.pivot = value;
        }

        // NOTE: ここだけEffectApi自体がデータ的に定義されてるので素朴にnew()してよい
        public SpriteEffectApi SpriteEffects { get; } = new();

        private float _transitionTime = 0f;
        
        //TODO: Transition自体にもEffectApiくらいの粒度のデータ型が欲しいかも

        /// <summary>
        /// 画像切り替えについて時間を積算しながら、エフェクトとして適用したい回転値を返す
        /// この呼び出しによってトランジションが完了した場合はtrueを返し、そうでなければfalseを返す
        /// </summary>
        /// <param name="deltaTime"></param>
        /// <param name="texture"></param>
        /// <param name="transitionStyle"></param>
        /// <returns></returns>
        public (Quaternion rot, bool isDone) DoTransition(float deltaTime, Texture2D texture, Sprite2DTransitionStyle transitionStyle)
        {
            if (transitionStyle == Sprite2DTransitionStyle.None)
            {
                _transitionTime = 0f;
                CurrentTransitionStyle = Sprite2DTransitionStyle.None;
                return (Quaternion.identity, false);
            }

            if (transitionStyle == Sprite2DTransitionStyle.Immediate)
            {
                _transitionTime = 0f;
                rawImage.texture = texture;
                CurrentTransitionStyle = Sprite2DTransitionStyle.None;
                return (Quaternion.identity, true);
            }
            
            // TEMP:
            // - いったん常にLeftFlip
            // - Transition中に呼ばれたケースを無視
            // - ユーザーがlocalRotationを適用してるケースも無視
            _transitionTime += deltaTime;
            var rate = Mathf.Clamp01(_transitionTime / TransitionDuration);
            if (rate < 0.5f)
            {
                return (Quaternion.Euler(0, 180f * rate, 0), false);
            }
            else
            {
                // NOTE: 0 .. 90deg から -90degにジャンプして-90 .. 0 に進める感じ
                rawImage.texture = texture;
                var rot = Quaternion.Euler(0, 180f * (rate - 1f), 0);
                if (_transitionTime >= TransitionDuration)
                {
                    _transitionTime = 0f;
                    CurrentTransitionStyle = Sprite2DTransitionStyle.None;
                    return (Quaternion.identity, true);
                }
                else
                {
                    return (rot, false);
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

        //TODO: なでなでに反応できてほしい気がするので、PointerMoveも検討したほうがいいかも
        // メインアバターに無いんかい！となるかもだが、アバターは自分だから撫でれんでも…という思想で行くとサブキャラだけ撫でられるのもアリそう
        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) 
            => _pointerEntered.OnNext(Unit.Default);
        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
            => _pointerExit.OnNext(Unit.Default);
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
            => _pointerClicked.OnNext(Unit.Default);

        void IPointerMoveHandler.OnPointerMove(PointerEventData eventData)
        {
            //do nothing: 移動量が分かることが必要なような…
        }

        public void Dispose()
        {
            CurrentTexture = null;
            foreach (var t in _textures.Values)
            {
                Destroy(t);
            }
            _textures.Clear();

            Destroy(gameObject);
        }
        
        /// <summary>
        /// 画像のロード処理はするが、ただちに表示するわけではなく、キャッシュにデータを乗せる
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        public TextureLoadResult Load(string fullPath)
        {
            if (_textures.ContainsKey(fullPath))
            {
                return TextureLoadResult.Success;
            }

            var loadResult = ApiUtils.TryGetTexture2D(fullPath, out var texture);
            if (loadResult is TextureLoadResult.Success)
            {
                _textures[fullPath] = texture;
            }
            
            return loadResult;
        }

        /// <summary>
        /// 必要なら画像のロードを行ったうえで表示する
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public TextureLoadResult Show(string fullPath, Sprite2DTransitionStyle style)
        {
            if (!_textures.ContainsKey(fullPath))
            {
                var loadResult = Load(fullPath);
                if (loadResult is not TextureLoadResult.Success)
                {
                    return loadResult;
                }
            }

            CurrentTexture = _textures[fullPath];
            CurrentTransitionStyle = style;
            
            return TextureLoadResult.Success;
        }
        
        public void SetActive(bool active) => gameObject.SetActive(active);
    }
}
