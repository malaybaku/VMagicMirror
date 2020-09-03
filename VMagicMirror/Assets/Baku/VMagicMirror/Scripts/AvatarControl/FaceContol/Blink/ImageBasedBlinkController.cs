using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// <see cref="FaceTracker"/>の入力からまばたき値を出力するやつ
    /// </summary>
    public class ImageBasedBlinkController : MonoBehaviour
    {
        private readonly RecordBlinkSource _blinkSource = new RecordBlinkSource();
        public IBlinkSource BlinkSource => _blinkSource;
        
        [Inject] private FaceTracker _faceTracker = null;
        
        [Tooltip("ブレンドシェイプを変化させていく速度ファクター")]
        [SerializeField]
        private float speedFactor = 12f;
        
        [Tooltip("瞬き速度を減速させるダンピング")]
        [SerializeField]
        private float speedDumpFactor = 0.85f;
        
        [Tooltip("瞬きの基本的なアップデート時間間隔。短いほど追従性がよい")]
        [SerializeField]
        private float blinkUpdateTimeScale = 0.2f;

        [Tooltip("この値以上のBlink値だったら目が完全に閉じているものと扱う")]
        [SerializeField] private float closeThreshold = 0.8f;
        
        public float blinkUpdateInterval = 0.3f;
        private float _count = 0;

        //TODO: ここ以前にT5_4Vやったときと同じで、閉じ方向と開き方向の速度を変えると良いかも。
        
        //一定間隔で_latestな値をコピーしてきた、当面ターゲットとすべきまばたきブレンドシェイプの値
        private float _leftBlinkTarget = 0f;
        private float _rightBlinkTarget = 0f;
        
        //_current(Left|Right)Blinkをターゲット値に持って行くときの速度。PD制御チックに動かす為に使います
        private float _leftBlinkSpeed = 0f;
        private float _rightBlinkSpeed = 0f;

        private float _latestFilteredLeft = 0f;
        private float _latestFilteredRight = 0f;

        private void Update()
        {
            _count -= Time.deltaTime;
            if (_count < 0)
            {
                _leftBlinkTarget = _faceTracker.EyeOpen.LeftEyeBlink;
                _rightBlinkTarget =  _faceTracker.EyeOpen.RightEyeBlink;
                _count = blinkUpdateInterval;                
            }

            float leftSpeed = (_leftBlinkTarget - _latestFilteredLeft) / blinkUpdateTimeScale;
            float rightSpeed = (_rightBlinkTarget - _latestFilteredRight) / blinkUpdateTimeScale;

            _leftBlinkSpeed = Mathf.Lerp(_leftBlinkSpeed, leftSpeed, speedFactor * Time.deltaTime);
            _rightBlinkSpeed = Mathf.Lerp(_rightBlinkSpeed, rightSpeed, speedFactor * Time.deltaTime);

            _leftBlinkSpeed *= speedDumpFactor;
            _rightBlinkSpeed *= speedDumpFactor;

            _latestFilteredLeft += _leftBlinkSpeed * Time.deltaTime;
            _latestFilteredRight += _rightBlinkSpeed * Time.deltaTime;

            _blinkSource.Left = (_latestFilteredLeft > closeThreshold) ? 1.0f : _latestFilteredLeft;
            _blinkSource.Right = (_latestFilteredRight > closeThreshold) ? 1.0f : _latestFilteredRight;
        }
    }
}
