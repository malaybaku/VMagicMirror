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
        [Range(0f, 0.4f)] [SerializeField] private float eyeMapMin = 0.2f;
        [Range(0.6f, 1f)] [SerializeField] private float eyeMapMax = 0.8f;

        [Tooltip("eyeSquintのブレンドシェイプ値が1に近いほど、BlinkL/Rをこの値に近づける")]
        [Range(0f, 1f)] [SerializeField] private float blinkValueOnSquint = 0.5f;
        
        [Tooltip("目が開く方向へブレンドシェイプ値を変更するとき、60FPSの1フレームあたりで変更できる値の上限")]
        [SerializeField] private float blinkOpenSpeedMax = 0.1f;
        
        [Inject] private ExternalTrackerDataSource _externalTracker = null;
        
        private readonly RecordBlinkSource _blinkSource = new RecordBlinkSource();
        public IBlinkSource BlinkSource => _blinkSource;
        
        private void Update()
        {
            float subLimit = blinkOpenSpeedMax * Time.deltaTime * 60f;
            var eye = _externalTracker.CurrentSource.Eye;

            //NOTE: ちょっとややこしいが、VMMはミラー的な表示を前提にしているため、
            //eye側のデータはデフォルトが左右反転している。そこまで踏まえつつ場合分けするとこうなる
            var rawLeftBlink = _externalTracker.DisableHorizontalFlip ? eye.RightBlink : eye.LeftBlink;
            var rawLeftSquint = _externalTracker.DisableHorizontalFlip ? eye.RightSquint : eye.LeftSquint;
            
            var rawRightBlink = _externalTracker.DisableHorizontalFlip ? eye.LeftBlink : eye.RightBlink;
            var rawRightSquint = _externalTracker.DisableHorizontalFlip ? eye.LeftSquint : eye.RightSquint;
            

            float left = MapClamp(rawLeftBlink);
            if (left < 0.9f)
            {
                left = Mathf.Lerp(left, blinkValueOnSquint, rawLeftSquint);
            }
            //NOTE: 開くほうは速度制限があるけど閉じるほうは一瞬でいい、という方式。右目も同様。
            left = Mathf.Clamp(left, _blinkSource.Left - subLimit, 1.0f);
            _blinkSource.Left = Mathf.Clamp01(left);

            float right = MapClamp(rawRightBlink);
            if (right < 0.9f)
            {
                Mathf.Lerp(right, blinkValueOnSquint, rawRightSquint);
            }
            right = Mathf.Clamp(right, _blinkSource.Right - subLimit, 1.0f);
            _blinkSource.Right = Mathf.Clamp01(right);
        }

        //0-1の範囲の値をmin-maxの幅のなかにギュッとあれします
        private float MapClamp(float value) 
            => Mathf.Clamp01((value - eyeMapMin) / (eyeMapMax - eyeMapMin));
    } 
}
