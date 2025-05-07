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
        public Sprite2DTransitionStyle Style { get; set; }
        // NOTE: この値は0以下の場合、「1Fで実行してね」的なTransitionを表すように動く
        public float TransitionDuration { get; set; }

        public float Time { get; set; }
        /// <summary>
        /// Transition中に適用したいテクスチャ名があるうちはそのキーになり、適用したら空文字列になるような値
        /// </summary>
        public Texture2D UnAppliedTexture { get; set; }
        public bool HasUnAppliedTexture => UnAppliedTexture != null;
        
        // NOTE: 実質 init set だが、ガチガチにするほどでもないのでsetterが外から見えている
        public bool IsDefaultSprites { get; set; }

        public float Rate => Time / TransitionDuration;
        // NOTE: Immediateフラグが立ってる場合、「Durationは0かもしれんが未完了」という意味になる
        public bool IsCompleted =>
            Style is Sprite2DTransitionStyle.None ||
            (!IsImmediate && Time >= TransitionDuration);
        
        // NOTE: このフラグが立っている場合、ただちに未適用テクスチャを適用し、遷移状態を Transition.None に上書きすることが望ましい
        public bool IsImmediate => Style is Sprite2DTransitionStyle.Immediate || TransitionDuration <= 0f;

        /// <summary>
        /// デフォルト以外の、1枚ごとに個別で取り扱える画像へのトランジションを表すインスタンスを生成する
        /// </summary>
        /// <param name="style"></param>
        /// <param name="texture"></param>
        /// <param name="duration">
        /// </param>
        /// <returns></returns>
        public static BuddySprite2DInstanceTransition CreateCustom(
            Sprite2DTransitionStyle style, Texture2D texture, float duration
            ) => new()
        {
            Style = style,
            TransitionDuration = duration,
            IsDefaultSprites = false,
            Time = 0f,
            UnAppliedTexture = texture,
        };

        /// <summary>
        /// デフォルト立ち絵へのトランジションを表すインスタンスを生成する
        /// </summary>
        /// <param name="style"></param>
        /// <param name="texture"></param>
        /// <param name="duration"></param>
        /// <returns></returns>
        public static BuddySprite2DInstanceTransition CreateDefault(
            Sprite2DTransitionStyle style, Texture2D texture, float duration
            ) => new()
        {
            Style = style,
            TransitionDuration = duration,
            IsDefaultSprites = true,
            Time = 0f,
            UnAppliedTexture = texture,
        };
        
        public static BuddySprite2DInstanceTransition None => new()
        {
            Style = Sprite2DTransitionStyle.None,
            TransitionDuration = 0f,
            IsDefaultSprites = false,
            Time = 0f,
            UnAppliedTexture = null,
        };
    }
    
    public class BuddySprite2DInstance : MonoBehaviour, 
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerMoveHandler
    {
        [SerializeField] private BuddyTransform2DInstance transform2DInstance;
        [SerializeField] private RectTransform effectorRectTransform;
        [SerializeField] private RawImage rawImage;

        public BuddyTransform2DInstance GetTransform2DInstance() => transform2DInstance;
        
        public BuddyPresetResources PresetResources { get; set; }
        
        /// <summary>
        /// 実際に表示されているテクスチャがデフォルト立ち絵由来のものならtrueになる。トランジションの開始時点では変化しないことに注意
        /// </summary>
        public bool IsDefaultSpritesActive { get; private set; }
        
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

        public BuddyDefaultSpritesSettingInstance DefaultSpritesSetting { get; } = new();

        public BuddyDefaultSpritesInstance DefaultSpritesInstance { get; } = new(false);
        public BuddyDefaultSpritesUpdater DefaultSpritesUpdater { get; set; }

        public string BuddyId { get; set; }

        /// <summary>
        /// 実際に表示されるテクスチャを更新する。とくに、第二引数によってテクスチャがデフォルト立ち絵のものであるかどうかも明示的に指定する。
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="isDefaultSpritesTexture"></param>
        public void SetTexture(Texture2D texture, bool isDefaultSpritesTexture)
        {
            rawImage.texture = texture;
            IsDefaultSpritesActive = isDefaultSpritesTexture;
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
            foreach (var t in _textures.Values)
            {
                Destroy(t);
            }
            _textures.Clear();
            DefaultSpritesInstance.Dispose();
            DefaultSpritesUpdater.Dispose();

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
        /// <param name="duration"></param>
        /// <returns></returns>
        public TextureLoadResult Show(string fullPath, Sprite2DTransitionStyle style, float duration)
        {
            if (!_textures.ContainsKey(fullPath))
            {
                var loadResult = Load(fullPath);
                if (loadResult is not TextureLoadResult.Success)
                {
                    return loadResult;
                }
            }

            Transition = BuddySprite2DInstanceTransition.CreateCustom(style, _textures[fullPath], duration);
            
            return TextureLoadResult.Success;
        }

        public TextureLoadResult ShowPreset(string presetName, Sprite2DTransitionStyle style, float duration)
        {
            if (PresetResources == null)
            {
                throw new InvalidOperationException("PresetResources is not initialized");
            }
            
            if (PresetResources.TryGetTexture(presetName, out var texture))
            {
                Transition = BuddySprite2DInstanceTransition.CreateCustom(style,texture, duration);
                return TextureLoadResult.Success;
            }

            return TextureLoadResult.FailureFileNotFound;
        }
        
        public void SetActive(bool active) => gameObject.SetActive(active);

        public TextureLoadResult SetupDefaultSprites(
            string defaultPath,
            string blinkPath,
            string mouthOpenPath,
            string blinkMouthOpenPath
            )
        {
            // TODO: 失敗判定
            var defaultResult = ApiUtils.TryGetTexture2D(defaultPath, out var defaultTexture);
            var blinkResult = ApiUtils.TryGetTexture2D(blinkPath, out var blinkTexture);
            var mouthOpenResult =  ApiUtils.TryGetTexture2D(mouthOpenPath, out var mouthOpenTexture);
            var blinkMouthOpenResult = ApiUtils.TryGetTexture2D(blinkMouthOpenPath, out var blinkMouthOpenTexture);

            // 一つでも失敗してたら続行しない (※defaultさえ通ってれば通す…みたいなのもアリかも。)
            if (defaultResult is not TextureLoadResult.Success) return defaultResult;
            if (blinkResult is not TextureLoadResult.Success) return blinkResult;
            if (mouthOpenResult is not TextureLoadResult.Success) return mouthOpenResult;
            if (blinkMouthOpenResult is not TextureLoadResult.Success) return blinkMouthOpenResult;
            
            DefaultSpritesInstance.SetupTexture(
                true,
                defaultTexture,
                blinkTexture,
                mouthOpenTexture,
                blinkMouthOpenTexture
                );
            return TextureLoadResult.Success;
        }

        public void SetupDefaultSpritesByPreset()
        {
            var sprites = PresetResources.GetDefaultSprites();
            DefaultSpritesInstance.SetupTexture(
                false,
                sprites.DefaultTexture,
                sprites.BlinkTexture,
                sprites.MouthOpenTexture,
                sprites.BlinkMouthOpenTexture
                );
        }

        public void ShowDefaultSprites(Sprite2DTransitionStyle style, float duration)
        {
            Transition = BuddySprite2DInstanceTransition.CreateDefault(
                style, DefaultSpritesInstance.CurrentTexture, duration
                );
        }

        public void UpdateDefaultSpritesTexture()
        {
            DefaultSpritesUpdater.Update();
            DefaultSpritesInstance.SetState(DefaultSpritesUpdater.State);
            if (IsDefaultSpritesActive)
            {
                // NOTE: デフォルト絵を当ててよい状態なら実際に当てる。
                // この呼び出しで実際にテクスチャが変わるフレームは疎で、まばたきのon/offがたまーに効く程度
                SetTexture(DefaultSpritesInstance.CurrentTexture, true);
            }
        }
    }
}
