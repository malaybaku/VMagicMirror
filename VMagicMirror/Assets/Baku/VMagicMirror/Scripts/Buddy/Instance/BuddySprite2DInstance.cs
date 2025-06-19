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
        [SerializeField] private Shadow shadow;

        public BuddyTransform2DInstance GetTransform2DInstance() => transform2DInstance;
        
        public BuddyPresetResources PresetResources { get; set; }
        
        /// <summary>
        /// 実際に表示されているテクスチャがデフォルト立ち絵由来のものならtrueになる。トランジションの開始時点では変化しないことに注意
        /// </summary>
        public bool IsDefaultSpritesActive { get; private set; }
        
        // NOTE: この値は BuddySpriteUpdater が更新してよい前提で公開される
        public BuddySprite2DInstanceTransition Transition { get; set; } = BuddySprite2DInstanceTransition.None;

        private readonly Dictionary<string, Texture2D> _textures = new();

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

        /// <summary>
        /// 下記のような特徴のRectを返すことで、セリフの基準位置を与える
        /// - 基準位置は原則としてparentの位置、なければ自分自身の位置
        /// - LocalRotationがない状態に相当
        /// - Size, Scale, Pivotは考慮される
        /// </summary>
        /// <returns></returns>
        public Rect GetStableRect()
        {
            // NOTE: かなり荒っぽいが。いったん基準姿勢を取らせることで大筋の基準位置を取りにいく
            var rot = transform2DInstance.LocalRotation;
            var pos = transform2DInstance.LocalPosition;
            
            var effectorPos = effectorRectTransform.anchoredPosition;
            var effectorRot = effectorRectTransform.localRotation;
            var effectorScale = effectorRectTransform.localScale;
            
            try
            {
                // NOTE: 親要素をまったく使わずに位置制御しているサブキャラの場合は現在位置をリスペクトする
                if (transform2DInstance.HasParentTransform2DInstance())
                {
                    transform2DInstance.LocalPosition = Vector2.zero;
                }
                transform2DInstance.LocalRotation = Quaternion.identity;

                effectorRectTransform.anchoredPosition = Vector2.zero;
                effectorRectTransform.localRotation = Quaternion.identity;
                effectorRectTransform.localScale = Vector3.one;
                
                var rect = RectTransformAreaUtil.GetLocalRectForWorldSpaceUi(
                    transform2DInstance.ParentCanvas, transform2DInstance.ContentTransform
                );

                return new Rect(
                    rect.position + 
                        new Vector2(BuddySpriteCanvas.CanvasWidthDefault / 2, BuddySpriteCanvas.CanvasHeightDefault / 2),
                    rect.size
                );
            }
            finally
            {
                transform2DInstance.LocalPosition = pos;
                transform2DInstance.LocalRotation = rot;
                
                effectorRectTransform.anchoredPosition = effectorPos;
                effectorRectTransform.localRotation = effectorRot;
                effectorRectTransform.localScale = effectorScale;
            }
        }

        // NOTE: ここだけEffectApi自体がデータ的に定義されてるので素朴にnew()してよい
        public SpriteEffectApi SpriteEffects { get; } = new();

        public BuddyDefaultSpritesSettingInstance DefaultSpritesSetting { get; } = new();

        public BuddyDefaultSpritesInstance DefaultSpritesInstance { get; } = new(false);
        public BuddyDefaultSpritesUpdater DefaultSpritesUpdater { get; set; }

        public BuddyFolder BuddyFolder { get; set; }

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

        public (TextureLoadResult, TextureLoadResult, TextureLoadResult, TextureLoadResult) SetupDefaultSprites(
            string defaultPath,
            string blinkPath,
            string mouthOpenPath,
            string blinkMouthOpenPath
            )
        {
            var defaultResult = ApiUtils.TryGetTexture2D(defaultPath, out var defaultTexture);
            var blinkResult = ApiUtils.TryGetTexture2D(blinkPath, out var blinkTexture);
            var mouthOpenResult =  ApiUtils.TryGetTexture2D(mouthOpenPath, out var mouthOpenTexture);
            var blinkMouthOpenResult = ApiUtils.TryGetTexture2D(blinkMouthOpenPath, out var blinkMouthOpenTexture);

            // 全て成功ならSetupTextureを行ってよい
            if (defaultResult is TextureLoadResult.Success &&
                blinkResult is TextureLoadResult.Success &&
                mouthOpenResult is TextureLoadResult.Success &&
                blinkMouthOpenResult is TextureLoadResult.Success)
            {
                DefaultSpritesInstance.SetupTexture(
                    true,
                    defaultTexture,
                    blinkTexture,
                    mouthOpenTexture,
                    blinkMouthOpenTexture
                );
            }

            return (defaultResult, blinkResult, mouthOpenResult, blinkMouthOpenResult);
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

        public bool ShowDefaultSprites(Sprite2DTransitionStyle style, float duration)
        {
            Transition = BuddySprite2DInstanceTransition.CreateDefault(
                style, DefaultSpritesInstance.CurrentTexture, duration
                );
            return DefaultSpritesInstance.HasValidSetup;
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
        
        // NOTE: 下記はいずれもScript APIからではなく、BuddySprite2DInstanceの一覧を持ってるrepoから呼び出す想定
        public void SetShadowEnabled(bool enable) => shadow.enabled = enable;
        public void SetShadowColor(Color color) => shadow.effectColor = color;
        
        public BuddyTalkTextInstance CreateTalkTextInstance()
        {
            // NOTE: ちょっと手段が汚いが、prefabを持っているのが親Canvasなので親に任す方向で…
            return GetComponentInParent<BuddySpriteCanvas>()
                .CreateTalkTextInstance(this);
        }
    }

    public static class RectTransformAreaUtil
    {
        private static readonly Vector3[] WorldCorners = new Vector3[4];

        public static Rect GetLocalRectForWorldSpaceUi(Canvas canvas, RectTransform rt)
        {
            var canvasRT = (RectTransform)canvas.transform;
            // worldCorners: [0]=BL, [1]=TL, [2]=TR, [3]=BR
            rt.GetWorldCorners(WorldCorners);

            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            for (var i = 0; i < 4; i++)
            {
                var local = canvasRT.InverseTransformPoint(WorldCorners[i]);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            // 左下 (min) を原点に、幅・高さを計算
            var localRect = new Rect(min, max - min);
            return localRect;
        }        
        
        /// <summary>
        /// World Space の Canvas 内に、RectTransform rt がマージンを考慮して完全に収まっているかを判定します。
        /// margin はピクセル単位で指定し、Canvas.scaleFactor を用いてローカル単位に変換されます。
        /// </summary>
        public static bool IsContainedInCanvas(Canvas canvas, RectTransform rt, float margin)
        {
            var canvasRectT = (RectTransform)canvas.transform;
            // Canvas のローカル矩形を取得し、マージン分だけ内側に縮小
            var localRect = canvasRectT.rect;
            var localMargin = margin / canvas.scaleFactor;
            localRect.xMin += localMargin;
            localRect.yMin += localMargin;
            localRect.xMax -= localMargin;
            localRect.yMax -= localMargin;

            // 各コーナーを Canvas のローカル座標に変換し、内包判定
            rt.GetWorldCorners(WorldCorners);
            for (var i = 0; i < 4; i++)
            {
                var worldPt = WorldCorners[i];
                var localPt = canvasRectT.InverseTransformPoint(worldPt);
                if (!localRect.Contains((Vector2)localPt))
                {
                    Debug.Log($"local rect {localRect} does not contain point {localPt} from world point {worldPt}");
                    return false;
                }
            }

            return true;
        }
        
        /// <summary>
        /// 指定した RectTransform の中心が、Canvas のローカル座標系で右側にあるかを判定します。
        /// </summary>
        /// <param name="canvas">対象の Canvas</param>
        /// <param name="rt">判定したい RectTransform（Canvas の子階層にあること）</param>
        /// <returns>
        /// 中心点が Canvas のローカル X = 0 の位置より大きければ true（右側）、それ以外は false（左側またはちょうど中央）
        /// </returns>
        public static bool IsOnRightSide(Canvas canvas, RectTransform rt)
        {
            var canvasRectT = (RectTransform)canvas.transform;
            var worldCenter = rt.TransformPoint(rt.rect.center);
            var localCenter = canvasRectT.InverseTransformPoint(worldCenter);

            // ローカル X が正なら右側
            return localCenter.x > 0f;
        }
    }
}
