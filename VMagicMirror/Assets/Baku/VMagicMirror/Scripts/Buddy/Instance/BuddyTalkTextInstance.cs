using System;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// 実質的に ITalkText の実装をするクラス。
    /// <see cref="BuddySprite2DInstance"/> 1つに対して1インスタンスが存在するのが期待値だが、ヒエラルキーの関係性は保証しない。
    /// </summary>
    public class BuddyTalkTextInstance : MonoBehaviour
    {
        private const float DefaultWindowWidth = 1920f;

        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private RectTransform rt;
        [SerializeField] private RectTransform visualAnchor;
        [SerializeField] private BuddyTalkTextClickArea clickArea;
        [SerializeField] private BuddyTalkTextScroller talkTextScroller;
        
        // アイテムをDequeue扱いするとtrueになり、そのアイテムの処理が終わるとfalseに戻る。
        // 連続でアイテムを処理する場合も一瞬だけfalseになってtrueに戻る
        private bool _hasCurrentItem = false;

        // NOTE: なんとなく居心地悪いので text.text 以外のとこにマスターデータを持っておく
        private string _currentText = "";
        private float _currentTextElapsedTime = 0f;

        private (int width, int height) _screenSize;
        
        /// <summary>
        /// このインスタンスが紐づくSprite2D。オブジェクトの生成後、直ちにsetterが呼ばれる想定
        /// </summary>
        public BuddySprite2DInstance Sprite2DInstance { get; set; }
 
        public BuddyFolder BuddyFolder => Sprite2DInstance.BuddyFolder;
        private BuddyId BuddyId => BuddyFolder.BuddyId;
        
        private readonly List<TalkTextItemInternal> _queuedItems = new();
        public IReadOnlyList<TalkTextItemInternal> QueuedItems => _queuedItems;

        // NOTE: IO<T>を最終的にScriptEventInvokerの発火に帰着させたい
        private readonly Subject<TalkTextItemInternal> _itemDequeued = new();
        public IObservable<TalkTextItemInternal> ItemDequeued => _itemDequeued;

        private readonly Subject<TalkTextItemInternal> _itemFinished = new();
        public IObservable<TalkTextItemInternal> ItemFinished => _itemFinished;

        private void Start()
        {
            clickArea.Clicked
                .Subscribe(_ => FinishCurrentItem())
                .AddTo(this);
        }
        
        public void Dispose()
        {
            //NOTE: 今のところ何もない (gameObject自体が破棄されればOKなため)
        }

        public void UpdateTextState(float deltaTime)
        {
            UpdateCurrentText(deltaTime);
            UpdateSize();
            UpdatePosition();
        }

        private void UpdateCurrentText(float deltaTime)
        {
            if (_queuedItems.Count == 0)
            {
                if (gameObject.activeSelf)
                {
                    text.text = "";
                    gameObject.SetActive(false);
                }
                return;
            }

            var currentItem = _queuedItems[0];
            if (!_hasCurrentItem)
            {
                _itemDequeued.OnNext(currentItem);
                _hasCurrentItem = true;
                if (currentItem.Type is TalkTextItemTypeInternal.Text)
                {
                    talkTextScroller.ProceedToNextText();
                }
            }
            
            _currentTextElapsedTime += deltaTime;

            // NOTE:
            // - ScheduledDuration <= 0f の判定も必要だが、これは1つ目の判定でついでにチェックしている
            // - 次のアイテムに切り替わるときテキストをリセットせず、逆にフルで表示する (Wait中にテキストが残るようにしておく)
            if (_currentTextElapsedTime >= currentItem.ScheduledDuration)
            {
                FinishCurrentItem();
                return;
            }

            if (currentItem.Type == TalkTextItemTypeInternal.Wait)
            {
                // テキストを変更せずにそのまま待つ
                return;
            }

            var charPerSecond = currentItem.Text.Length / currentItem.ScheduledDuration;
            var charCount = Mathf.FloorToInt(_currentTextElapsedTime * charPerSecond);
            if (charCount != _currentText.Length)
            {
                _currentText = currentItem.Text[..charCount];
                text.text = _currentText;
            }

            // ここに到達する場合、テキストを表示し始めているのでオブジェクトは表示されていてほしい
            gameObject.SetActive(true);
        }

        private void UpdateSize()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }
            
            var screenSize = (Screen.width, Screen.height);
            if (_screenSize == screenSize)
            {
                return;
            }
            _screenSize = screenSize;
            
            // 逆数なことに注意。CanvasScalerの子要素だけどサイズの見えを保ちたい…というモチベーションでこうなっている
            rt.localScale = Vector3.one * (DefaultWindowWidth / _screenSize.width);
        }

        private void UpdatePosition()
        {
            if (!gameObject.activeInHierarchy || Sprite2DInstance == null)
            {
                return;
            }

            // NOTE: Bottom側も使ってウィンドウの縁を徹底的に避けるような実装もアリだが、煩雑な割に価値が無さそうなのでやらない
            var rect = Sprite2DInstance.GetStableRect();
            if (RectTransformAreaUtil.IsOnRightSide(
                    Sprite2DInstance.GetTransform2DInstance().ParentCanvas,
                    (RectTransform)Sprite2DInstance.transform
                ))
            {
                PlaceTextArea(rect, TextAreaPlacement.TopLeft);
            }
            else
            {
                PlaceTextArea(rect, TextAreaPlacement.TopRight);
            }
        }

        private void FinishCurrentItem()
        {
            if (!_hasCurrentItem || _queuedItems.Count == 0)
            {
                return;
            }
            
            var item = _queuedItems[0];
            _queuedItems.RemoveAt(0);
            _currentTextElapsedTime = 0f;
            if (item.Type == TalkTextItemTypeInternal.Text)
            {
                _currentText = item.Text;
                text.text = _currentText;
            }
            _itemFinished.OnNext(item);
            _hasCurrentItem = false;
        }

        public void AddTalkItem(string content, string key, float textDuration) 
            => _queuedItems.Add(TalkTextItemInternal.CreateText(BuddyId, content, key, textDuration));

        public void AddWaitItem(string key, float duration)
            => _queuedItems.Add(TalkTextItemInternal.CreateWait(BuddyId, key, duration));

        public void Clear(bool includeCurrentItem)
        {
            if (includeCurrentItem)
            {
                FinishCurrentItem();
            }

            _queuedItems.Clear();
            gameObject.SetActive(false);
        }
        
        
        enum TextAreaPlacement
        {
            TopRight,
            TopLeft,
            BottomRight,
            BottomLeft,
        }

        private void PlaceTextArea(Rect sprite2DRect, TextAreaPlacement placement)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            // NOTE: Spriteのカドに配置するのはあまり筋が良くないと思うので、上下方向をずらして少し中央側に持っていく
            var yTop = Mathf.Lerp(sprite2DRect.yMin, sprite2DRect.yMax, 0.7f);
            var yBottom = Mathf.Lerp(sprite2DRect.yMin, sprite2DRect.yMax, 0.3f);
            
            switch (placement)
            {
                case TextAreaPlacement.TopRight:
                    rt.anchoredPosition = new Vector2(sprite2DRect.xMax, yTop);
                    visualAnchor.pivot = new Vector2(0, 0);
                    break;
                case TextAreaPlacement.TopLeft:
                    rt.anchoredPosition = new Vector2(sprite2DRect.xMin, yTop);
                    visualAnchor.pivot = new Vector2(1, 0);
                    break;
                case TextAreaPlacement.BottomRight:
                    rt.anchoredPosition = new Vector2(sprite2DRect.xMax, yBottom);
                    visualAnchor.pivot = new Vector2(0, 1);
                    break;
                case TextAreaPlacement.BottomLeft:
                    rt.anchoredPosition = new Vector2(sprite2DRect.xMin, yBottom);
                    visualAnchor.pivot = new Vector2(1, 1);
                    break;
                default:
                    break;
            }
        }
    }
}
