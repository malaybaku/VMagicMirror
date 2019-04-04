using UnityEngine;
using UniRx;
using VRM;

namespace Baku.VMagicMirror
{
    public class FaceBlendShapeController : MonoBehaviour
    {
        [SerializeField]
        private FaceDetector faceDetector = null;

        [SerializeField]
        [Tooltip("指定した値だけ目の開閉が変化しないうちは目の開閉度を変えない")]
        private float blinkMoveThreshold = 0.2f;

        [SerializeField]
        [Tooltip("この値よりBlink値が小さい場合は目が開き切ったのと同様に扱う")]
        private float blinkForceMinThreshold = 0.1f;

        [SerializeField]
        [Tooltip("この値よりBlink値が大きい場合は目が閉じ切ったのと同様に扱う")]
        private float blinkForceMaxThreshold = 0.9f;

        public float blinkUpdateInterval = 0.3f;
        private float _count = 0;

        public float speedFactor = 0.2f;

        private VRMBlendShapeProxy _blendShapeProxy = null;

        private float _prevLeftBlink = 0f;
        private float _prevRightBlink = 0f;

        private float _leftBlinkTarget = 0f;
        private float _rightBlinkTarget = 0f;

        private float _latestLeftBlinkInput = 0f;
        private float _latestRightBlinkInput = 0f;

        public void Initialize(VRMBlendShapeProxy proxy)
        {
            _blendShapeProxy = proxy;
            //瞬きとは競合するので上書きする
            if (proxy.GetComponent<VRMBlink>() is VRMBlink blink)
            {
                blink.enabled = false;
            }
        }

        public void DisposeProxy()
        {
            _blendShapeProxy = null;
        }

        private void Start()
        {
            ShowAllCameraInfo();

            faceDetector.FaceParts
                .LeftEye
                .EyeOpenValue
                .Subscribe(v => OnLeftEyeOpenValueChanged(v));

            faceDetector.FaceParts
                .RightEye
                .EyeOpenValue
                .Subscribe(v => OnRightEyeOpenValueChanged(v));
        }

        private void Update()
        {
            if (_blendShapeProxy == null)
            {
                return;
            }

            _count -= Time.deltaTime;
            if (_count < 0)
            {
                _leftBlinkTarget = _latestLeftBlinkInput;
                _rightBlinkTarget = _latestRightBlinkInput;
                _count = blinkUpdateInterval;                
            }
            
            float left = Mathf.Lerp(_prevLeftBlink, _leftBlinkTarget, speedFactor);
            _blendShapeProxy.ImmediatelySetValue(BlendShapePreset.Blink_L, left);
            _prevLeftBlink = left;

            float right = Mathf.Lerp(_prevRightBlink, _rightBlinkTarget, speedFactor);
            _blendShapeProxy.ImmediatelySetValue(BlendShapePreset.Blink_R, right);
            _prevRightBlink = right;
        }

        private void ShowAllCameraInfo()
        {
            foreach (var device in WebCamTexture.devices)
            {
                Debug.Log($"Webcam Device Name:{device.name}");
            }
        }

        //FaceDetector側では目の開き具合を出力しているので反転
        private void OnLeftEyeOpenValueChanged(float value)
        {
            _latestLeftBlinkInput = 1 - value;
            //if (value < blinkForceMinThreshold)
            //{
            //    _latestLeftBlinkInput = 0;
            //}
            //else if (value > blinkForceMaxThreshold)
            //{
            //    _latestLeftBlinkInput = 1;
            //}
            //else if (Mathf.Abs(_latestLeftBlinkInput - value) > blinkMoveThreshold)
            //{ 
            //    _latestLeftBlinkInput = 1 - value;
            //}
        }

        private void OnRightEyeOpenValueChanged(float value)
        {
            _latestRightBlinkInput = 1 - value;
            //if (value < blinkForceMinThreshold)
            //{
            //    _latestRightBlinkInput = 0;
            //}
            //else if (value > blinkForceMaxThreshold)
            //{
            //    _latestRightBlinkInput = 1;
            //}
            //else if (Mathf.Abs(_latestRightBlinkInput - value) > blinkMoveThreshold)
            //{
            //    _latestRightBlinkInput = 1 - value;
            //}
        }
    }
}
