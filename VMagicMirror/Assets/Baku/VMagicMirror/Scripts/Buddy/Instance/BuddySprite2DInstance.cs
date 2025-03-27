using System;
using System.Collections.Generic;
using Baku.VMagicMirror.Buddy.Api;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Baku.VMagicMirror.Buddy
{
    public struct BuddySprite2DInstanceTransition
    {
        // TODO: Transitionの種類によってDurationを変えたい
        // そもそも引数で受け取れるようにせい、ということかもしれないが
        private const float TransitionDuration = 0.8f;

        public Sprite2DTransitionStyle Style { get; set; }
        public float Time { get; set; }
        /// <summary>
        /// Transition中に適用したいテクスチャ名があるうちはそのキーになり、適用したら空文字列になるような値
        /// </summary>
        public string UnAppliedTextureKey { get; set; }
        public bool HasUnAppliedTextureKey => !string.IsNullOrEmpty(UnAppliedTextureKey);

        public float Rate => Time / TransitionDuration;
        public bool IsCompleted => Style is Sprite2DTransitionStyle.None || Time >= TransitionDuration;

        public static BuddySprite2DInstanceTransition Create(Sprite2DTransitionStyle style, string textureKey) => new()
        {
            Style = style,
            Time = 0f,
            UnAppliedTextureKey = textureKey,
        };
        
        public static BuddySprite2DInstanceTransition None => new()
        {
            Style = Sprite2DTransitionStyle.None,
            Time = 0f,
            UnAppliedTextureKey = "",
        };
    }
    
    public class BuddySprite2DInstance : MonoBehaviour, 
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerMoveHandler
    {
        [SerializeField] private BuddyTransform2DInstance transform2DInstance;
        [SerializeField] private RectTransform effectorRectTransform;
        [SerializeField] private RawImage rawImage;

        public BuddyTransform2DInstance GetTransform2DInstance() => transform2DInstance;
        
        // NOTE: CurrentTransitionStyleがNone以外な場合、このテクスチャが実際に表示されているとは限らない
        public Texture2D CurrentTexture { get; private set; }

        // NOTE: この値は BuddySpriteUpdater が更新してよい前提で公開される
        public BuddySprite2DInstanceTransition Transition { get; set; } = BuddySprite2DInstanceTransition.None;

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

        public Vector2 Size
        {
            get => rawImage.rectTransform.sizeDelta;
            set => rawImage.rectTransform.sizeDelta = value;
        }

        // NOTE: ここだけEffectApi自体がデータ的に定義されてるので素朴にnew()してよい
        public SpriteEffectApi SpriteEffects { get; } = new();
        
        public string BuddyId { get; set; }

        public void SetTexture(string key)
        {
            if (_textures.TryGetValue(key, out var texture))
            {
                rawImage.texture = texture;
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
            Transition = BuddySprite2DInstanceTransition.Create(style, fullPath);
            
            return TextureLoadResult.Success;
        }
        
        public void SetActive(bool active) => gameObject.SetActive(active);
    }
}
