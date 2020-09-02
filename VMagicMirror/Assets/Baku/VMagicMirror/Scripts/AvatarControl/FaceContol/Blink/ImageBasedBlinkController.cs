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
        private const float EyeCloseHeight = 0.02f;
        
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

        //顔トラッキングで得たとにかく最新の値
        private float _latestLeftBlinkInput = 0f;
        private float _latestRightBlinkInput = 0f;

        //一定間隔で_latestな値をコピーしてきた、当面ターゲットとすべきまばたきブレンドシェイプの値
        private float _leftBlinkTarget = 0f;
        private float _rightBlinkTarget = 0f;
        
        //_current(Left|Right)Blinkをターゲット値に持って行くときの速度。PD制御チックに動かす為に使います
        private float _leftBlinkSpeed = 0f;
        private float _rightBlinkSpeed = 0f;

        private float _latestFilteredLeft = 0f;
        private float _latestFilteredRight = 0f;

        // private void Start()
        // {
        //     _faceTracker.FaceParts
        //         .LeftEye
        //         .EyeOpenValue
        //         .Subscribe(OnLeftEyeOpenValueChanged);
        //
        //     _faceTracker.FaceParts
        //         .RightEye
        //         .EyeOpenValue
        //         .Subscribe(OnRightEyeOpenValueChanged);
        // }

        private void Update()
        {
            _latestLeftBlinkInput = _faceTracker.EyeOpen.LeftEyeBlink;
            _latestRightBlinkInput = _faceTracker.EyeOpen.RightEyeBlink;
            
            _count -= Time.deltaTime;
            if (_count < 0)
            {
                _leftBlinkTarget = _latestLeftBlinkInput;
                _rightBlinkTarget = _latestRightBlinkInput;
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

        //FaceDetector側では目の開き具合を出力しているのでブレンドシェイプ的には反転が必要なことに注意
        private void OnLeftEyeOpenValueChanged(float value) 
            => _latestLeftBlinkInput = GetEyeOpenValue(value);

        private void OnRightEyeOpenValueChanged(float value) 
            => _latestRightBlinkInput = GetEyeOpenValue(value);

        private float GetEyeOpenValue(float value)
        {
            float clamped = Mathf.Clamp(value, EyeCloseHeight, _faceTracker.CalibrationData.eyeOpenHeight);
            if (value > _faceTracker.CalibrationData.eyeOpenHeight)
            {
                return 0;
            }
            else
            {
                float range = _faceTracker.CalibrationData.eyeOpenHeight - EyeCloseHeight;
                //細目すぎてrangeが負になるケースも想定してる: このときはまばたき自体無効にしておく
                if (range < Mathf.Epsilon)
                {
                    return 0;
                }
                else
                {
                    return 1 - (clamped - EyeCloseHeight) / range;
                }
            }
        }
    }
}
