using System;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    [RequireComponent(typeof(Canvas))]
    public class BuddySpriteCanvas : MonoBehaviour
    {
        public const float CanvasWidthDefault = 1280f;
        public const float CanvasHeightDefault = 720f;

        [SerializeField] private Canvas canvas;
        [SerializeField] private BuddySprite2DInstance spriteInstancePrefab;
        [SerializeField] private BuddyManifestTransform2DInstance transform2DInstancePrefab;
        [SerializeField] private BuddyTalkTextInstance talkTextInstancePrefab;

        // NOTE: 画面前面アクセサリーとの整合性のためにもっと手前に持ってこないとダメかも
        [SerializeField] private float distanceFromCamera = 0.1f;
        [SerializeField] private float canvasWidth = 1280f;

        private readonly Subject<BuddySprite2DInstance> _spriteCreated = new();
        public IObservable<BuddySprite2DInstance> SpriteCreated => _spriteCreated;
        private readonly Subject<BuddyTalkTextInstance> _talkTextCreated = new();
        public IObservable<BuddyTalkTextInstance> TalkTextCreated => _talkTextCreated;
        
        public RectTransform RectTransform => (RectTransform)transform;

        public BuddySprite2DInstance CreateSpriteInstance(BuddyFolder folder, BuddySettingsRepository settings, AvatarFacialApiImplement avatarFacialApiImpl)
        {
            var result = Instantiate(spriteInstancePrefab, RectTransform);
            result.BuddyFolder = folder;
            result.PresetResources = _presetResources;
            result.DefaultSpritesUpdater = new BuddyDefaultSpritesUpdater(settings, avatarFacialApiImpl, result);
            result.DefaultSpritesUpdater.Initialize();
            _spriteCreated.OnNext(result);
            return result;
        }

        /// <summary>
        /// ScriptLoaderがスクリプトをロードしている段階で呼ぶことで、Buddyが使う事があるTransform2Dを生成してCanvas上に配置する。
        /// 呼び出し直後は位置もサイズも保証されないことに注意
        /// </summary>
        /// <returns></returns>
        public BuddyManifestTransform2DInstance CreateManifestTransform2DInstance()
        {
            return Instantiate(transform2DInstancePrefab, RectTransform);
        }

        public BuddyTalkTextInstance CreateTalkTextInstance(BuddySprite2DInstance sprite2DInstance)
        {
            // TODO?: セリフのSiblingをLast側にもってく必要があるかも (Sprite2Dが2つ以上あると気になってくる)
            // やる場合、SpriteInstanceやMT2Dの生成後もケアする必要がある
            var result = Instantiate(talkTextInstancePrefab, RectTransform);
            result.Sprite2DInstance = sprite2DInstance;
            _talkTextCreated.OnNext(result);
            return result;
        }
        
        private Camera _mainCamera;
        private BuddyPresetResources _presetResources;
        private (float fov, int windowWidth, int windowHeight) _canvasSizeStatus = (0f, 0, 0);
        
        [Inject]
        public void Construct(Camera mainCamera, BuddyPresetResources presetResources)
        {
            _mainCamera = mainCamera;
            _presetResources = presetResources;
        }

        private void Start()
        {
            canvas.worldCamera = _mainCamera;
            transform.SetParent(_mainCamera.transform);
            transform.localRotation = Quaternion.identity;
            transform.localPosition = Vector3.forward * distanceFromCamera;

            _canvasSizeStatus = (_mainCamera.fieldOfView, Screen.width, Screen.height);
            UpdateCanvasSize();
        }
        
        // NOTE: Updateだとタイミングが厳格でないときもあるが、UpdateCanvasSizeの呼び出し自体わりと低頻度な見込みなので気にしないでおく
        private void Update()
        {
            var canvasSizeStatus = (_mainCamera.fieldOfView, Screen.width, Screen.height);
            if (_canvasSizeStatus != canvasSizeStatus)
            {
                _canvasSizeStatus = canvasSizeStatus;
                UpdateCanvasSize();
            }
        }

        private void UpdateCanvasSize()
        {
            var canvasRect = (RectTransform) canvas.transform;
            
            var canvasHeight = canvasWidth * _canvasSizeStatus.windowHeight / _canvasSizeStatus.windowWidth;
            canvasRect.sizeDelta = new Vector2(canvasWidth, canvasHeight);

            // ワールドスケールで適用したいCanvasの縦幅と、決定済みのcanvasHeightからscaleが求まる
            var worldCanvasHeight = 2f * Mathf.Tan(_mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distanceFromCamera;
            var localScale = worldCanvasHeight / canvasHeight;
            canvas.transform.localScale = Vector3.one * localScale;
        }
    }
}
