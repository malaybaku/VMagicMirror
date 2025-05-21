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
        
        [Inject] private ExternalTrackerDataSource _externalTracker;
        
        private readonly RecordBlinkSource _blinkSource = new();
        // HACK: この値は「パーフェクトシンク使用中にまばたきアクションをしたかどうか」の検出目的でも見に来るため、
        // アバター自体のまばたき制御をパーフェクトシンクで行っている間も値は更新し続けるのが期待値
        public IBlinkSource BlinkSource => _blinkSource;

        public (float left, float right) CalculateBlinkValues(
            float rightBlink, float rightSquint, float leftBlink, float leftSquint)
        {
            //NOTE: ちょっとややこしいが、VMMはミラー的な表示を前提にしているため、
            //eye側のデータはデフォルトが左右反転している。そこまで踏まえつつ場合分けするとこうなる
            var rawLeftBlink = _externalTracker.DisableHorizontalFlip ? rightBlink : leftBlink;
            var rawLeftSquint = _externalTracker.DisableHorizontalFlip ? rightSquint : leftSquint;
            
            var rawRightBlink = _externalTracker.DisableHorizontalFlip ? leftBlink : rightBlink;
            var rawRightSquint = _externalTracker.DisableHorizontalFlip ? leftSquint : rightSquint;

            var left = MapClamp(rawLeftBlink);
            if (left < 0.9f)
            {
                left = Mathf.Lerp(left, blinkValueOnSquint, rawLeftSquint);
            }

            var right = MapClamp(rawRightBlink);
            if (right < 0.9f)
            {
                right = Mathf.Lerp(right, blinkValueOnSquint, rawRightSquint);
            }
            return (left, right);
        }
        
        private void Update()
        {
            var eye = _externalTracker.CurrentSource.Eye;
            var (left, right) = CalculateBlinkValues(
                eye.RightBlink, eye.RightSquint, eye.LeftBlink, eye.LeftSquint
            );
            
            //NOTE: 開くほうは速度制限があるけど閉じるほうは一瞬でいい、という方式。右目も同様。
            var subLimit = blinkOpenSpeedMax * Time.deltaTime * 60f;
            left = Mathf.Clamp(left, _blinkSource.Left - subLimit, 1.0f);
            right = Mathf.Clamp(right, _blinkSource.Right - subLimit, 1.0f);

            _blinkSource.Left = Mathf.Clamp01(left);
            _blinkSource.Right = Mathf.Clamp01(right);
        }

        //0-1の範囲の値をmin-maxの幅のなかにギュッとあれします
        private float MapClamp(float value) 
            => Mathf.Clamp01((value - eyeMapMin) / (eyeMapMax - eyeMapMin));
    } 
}
