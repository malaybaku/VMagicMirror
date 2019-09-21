using UnityEngine;
using UniRx;
using VRM;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// <see cref="FaceTracker"/>の入力からまばたき値を出力するやつ
    /// </summary>
    public class ImageBasedBlinkController : MonoBehaviour
    {
        private const float EyeCloseHeight = 0.02f;

        private static readonly BlendShapeKey BlinkLKey = new BlendShapeKey(BlendShapePreset.Blink_L);
        private static readonly BlendShapeKey BlinkRKey = new BlendShapeKey(BlendShapePreset.Blink_R);

        [SerializeField] private FaceTracker faceTracker = null;
        
        [Tooltip("ブレンドシェイプを変化させていく速度ファクター")]
        [SerializeField]
        private float speedFactor = 12f;
        
        public float blinkUpdateInterval = 0.3f;
        private float _count = 0;


        //顔トラッキングで得たとにかく最新の値
        private float _latestLeftBlinkInput = 0f;
        private float _latestRightBlinkInput = 0f;

        //一定間隔で_latestな値をコピーしてきた、当面ターゲットとすべきまばたきブレンドシェイプの値
        private float _leftBlinkTarget = 0f;
        private float _rightBlinkTarget = 0f;
        
        //スムージングとかやった状態のまばたきブレンドシェイプの値
        private float _currentLeftBlink = 0f;
        private float _currentRightBlink = 0f;

        public void Apply(VRMBlendShapeProxy proxy)
        {
            proxy.AccumulateValue(BlinkLKey, _currentLeftBlink);
            proxy.AccumulateValue(BlinkRKey, _currentRightBlink);
        }

        private void Start()
        {
            faceTracker.FaceParts
                .LeftEye
                .EyeOpenValue
                .Subscribe(OnLeftEyeOpenValueChanged);

            faceTracker.FaceParts
                .RightEye
                .EyeOpenValue
                .Subscribe(OnRightEyeOpenValueChanged);
        }

        private void Update()
        {
            _count -= Time.deltaTime;
            if (_count < 0)
            {
                _leftBlinkTarget = _latestLeftBlinkInput;
                _rightBlinkTarget = _latestRightBlinkInput;
                _count = blinkUpdateInterval;                
            }
            
            _currentLeftBlink = Mathf.Lerp(_currentLeftBlink, _leftBlinkTarget, speedFactor * Time.deltaTime);
            _currentRightBlink = Mathf.Lerp(_currentRightBlink, _rightBlinkTarget, speedFactor * Time.deltaTime);
        }

        //FaceDetector側では目の開き具合を出力しているのでブレンドシェイプ的には反転が必要なことに注意
        private void OnLeftEyeOpenValueChanged(float value) 
            => _latestLeftBlinkInput = GetEyeOpenValue(value);

        private void OnRightEyeOpenValueChanged(float value) 
            => _latestRightBlinkInput = GetEyeOpenValue(value);

        private float GetEyeOpenValue(float value)
        {
            float clamped = Mathf.Clamp(value, EyeCloseHeight, faceTracker.CalibrationData.eyeOpenHeight);
            if (value > faceTracker.CalibrationData.eyeOpenHeight)
            {
                return 0;
            }
            else
            {
                float range = faceTracker.CalibrationData.eyeOpenHeight - EyeCloseHeight;
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
