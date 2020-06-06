using UnityEngine;
using Zenject;
using Baku.VMagicMirror.ExternalTracker;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 外部トラッキングベースでまばたきするすごいやつだよ
    /// </summary>
    public class ExternalTrackerBlink : MonoBehaviour
    {
        //NOTE: チャタリング防止のパラメタを思いつきで書いてます…が、最初の実装では要らんわな。
        
        [Tooltip("この比率を超えたら目を閉じる")]
        [SerializeField] private float eyeCloseRate = 0.85f;

        [Tooltip("目を閉じたあと、この比率を下回ったら目を開く")]
        [SerializeField] private float eyeReopenRate = 0.75f;
        
        [Inject] private ExternalTrackerDataSource _externalTracker = null;
        
        private readonly RecordBlinkSource _blinkSource = new RecordBlinkSource();
        public IBlinkSource BlinkSource => _blinkSource;

        private bool _leftClosed = false;
        private bool _rightClosed = false;
        
        private void Update()
        {
            float left = _externalTracker.CurrentSource.Eye.LeftBlink;
            float right = _externalTracker.CurrentSource.Eye.RightBlink;

            if (_leftClosed)
            {
                if (left < eyeReopenRate)
                {
                    _leftClosed = false;
                    _blinkSource.Left = left;
                }
                else
                {
                    _blinkSource.Left = 1.0f;
                }
            }
            else
            {
                if (left > eyeCloseRate)
                {
                    _leftClosed = true;
                    _blinkSource.Left = 1.0f;
                }
                else
                {
                    _blinkSource.Left = left;
                }
            }

            if (_rightClosed)
            {
                if (right < eyeReopenRate)
                {
                    _rightClosed = false;
                    _blinkSource.Right = right;
                }
                else
                {
                    _blinkSource.Right = 1.0f;
                }
            }
            else
            {
                if (right > eyeCloseRate)
                {
                    _rightClosed = true;
                    _blinkSource.Right = 1.0f;
                }
                else
                {
                    _blinkSource.Right = right;
                }
            }


        }
    } 
}
