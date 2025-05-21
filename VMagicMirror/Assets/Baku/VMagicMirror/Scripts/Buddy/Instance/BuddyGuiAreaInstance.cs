using TMPro;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    // NOTE: WebViewとかで代替する方向に行くと不要になるかも
    public class BuddyGuiAreaInstance : MonoBehaviour
    {
        [SerializeField] private BuddyTransform2DInstance transform2DInstance;
        [SerializeField] private TextMeshProUGUI text = null;
        [SerializeField] private float textPerSecond = 10;

        public BuddyTransform2DInstance GetTransform2DInstance() => transform2DInstance;

        // 文字送り中だけtrueになる。何も表示してなかったり、文字送りが完全に完了したりしているとfalse
        private bool _isShowingContent;
        private string _content = "";
        private float _textShowTime;
        private string _currentText = "";
                
        private RectTransform Rt => (RectTransform)transform;

        private Canvas _parentCanvas;
        private Canvas ParentCanvas
        {
            get
            {
                if (_parentCanvas == null)
                {
                    _parentCanvas = GetComponentInParent<Canvas>();
                }
                return _parentCanvas;
            }
        }
        
        public Vector2 Position
        {
            get => Rt.anchorMin;
            set
            {
                Rt.anchorMin = value;
                Rt.anchorMax = value;
            }
        }

        public Vector2 Pivot
        {
            get => Rt.pivot;
            set => Rt.pivot = value;
        }

        private Vector2 _size;
        public Vector2 Size
        {
            get => _size;
            set
            {
                _size = value;
                var canvasSize = ((RectTransform)ParentCanvas.transform).rect.size;
                Rt.sizeDelta = Vector2.Scale(canvasSize, _size);
            }
        }

        public void SetActive(bool active) => gameObject.SetActive(active);

        public void ShowText(string content, bool immediate)
        {
            _content = content;
            _textShowTime = 0f;
            if (immediate)
            {
                _currentText = content;
                text.text = content;
                _isShowingContent = false;
                return;
            }

            text.text = "";
            _currentText = "";
            _isShowingContent = true;
        }

        private void Update()
        {
            if (!_isShowingContent)
            {
                return;
            }

            _textShowTime += Time.deltaTime;
            var charCount = (int)(_textShowTime * textPerSecond);
            if (_currentText.Length >= charCount)
            {
                return;
            }

            _currentText = _content.Substring(0, charCount);
            text.text = _currentText;
            if (_currentText == _content)
            {
                _isShowingContent = false;
            }
        }

        public void Dispose() => Destroy(gameObject);
    }
}
