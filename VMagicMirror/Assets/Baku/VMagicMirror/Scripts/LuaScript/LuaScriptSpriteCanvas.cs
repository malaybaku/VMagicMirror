using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.LuaScript
{
    [RequireComponent(typeof(Canvas))]
    public class LuaScriptSpriteCanvas : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private LuaScriptSpriteInstance scriptSpriteInstancePrefab;

        // NOTE: 画面前面アクセサリーとの整合性のためにもっと手前に持ってこないとダメかも
        [SerializeField] private float distanceFromCamera = 0.1f;
        [SerializeField] private float canvasWidth = 1280f;
        
        public RectTransform RectTransform => (RectTransform)transform;

        public LuaScriptSpriteInstance CreateInstance() 
            => Instantiate(scriptSpriteInstancePrefab, RectTransform);

        private Camera _mainCamera;
        private (float fov, int windowWidth, int windowHeight) _canvasSizeStatus = (0f, 0, 0);
        
        [Inject]
        public void Construct(Camera mainCamera)
        {
            _mainCamera = mainCamera;
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
            var worldCanvasHeight = Mathf.Tan(_mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * distanceFromCamera;
            var localScale = worldCanvasHeight / canvasHeight;
            canvas.transform.localScale = Vector3.one * localScale;
        }
    }
}
