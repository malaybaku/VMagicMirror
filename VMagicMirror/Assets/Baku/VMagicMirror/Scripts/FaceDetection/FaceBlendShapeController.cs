﻿using System.Collections;
using UnityEngine;
using UniRx;
using VRM;

namespace Baku.VMagicMirror
{
    public class FaceBlendShapeController : MonoBehaviour
    {
        private BlendShapeKey BlinkLKey { get; } = new BlendShapeKey(BlendShapePreset.Blink_L);
        private BlendShapeKey BlinkRKey { get; } = new BlendShapeKey(BlendShapePreset.Blink_R);
        private BlendShapeKey FunKey { get; } = new BlendShapeKey(BlendShapePreset.Fun);

        private const float EyeBlendShapeCloseThreshold = 0.7f;

        private const float EyeCloseHeight = 0.02f;

        private float _faceDefaultFunValue = 0.2f;
        public float FaceDefaultFunValue
        {
            get => _faceDefaultFunValue;
            set
            {
                if (Mathf.Abs(_faceDefaultFunValue - value) > Mathf.Epsilon)
                {
                    _faceDefaultFunValue = value;
                    _blendShapeProxy?.ImmediatelySetValue(FunKey, value);
                }
            }
        }

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

            //HACK: Funに表情を寄せて仏頂面を回避。やり過ぎると他モーフとの組み合わせで破綻することがあるので、やりすぎない。
            StartCoroutine(DelayedSetFunBlendShape());
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
            if (_blendShapeProxy == null || faceDetector.AutoBlinkDuringFaceTracking)
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

            _blendShapeProxy.ImmediatelySetValue(
                BlinkLKey, 
                left > EyeBlendShapeCloseThreshold ? 1.0f : left);
            _prevLeftBlink = left;

            float right = Mathf.Lerp(_prevRightBlink, _rightBlinkTarget, speedFactor);
            _blendShapeProxy.ImmediatelySetValue(
                BlinkRKey, 
                right > EyeBlendShapeCloseThreshold ? 1.0f : right
                );
            _prevRightBlink = right;
        }

        private void ShowAllCameraInfo()
        {
            foreach (var device in WebCamTexture.devices)
            {
                LogOutput.Instance.Write($"Webcam Device Name:{device.name}");
            }
        }

        //FaceDetector側では目の開き具合を出力しているので反転
        private void OnLeftEyeOpenValueChanged(float value)
        {
            SetEyeOpenValue(ref _latestLeftBlinkInput, value);
        }

        private void OnRightEyeOpenValueChanged(float value)
        {
            SetEyeOpenValue(ref _latestRightBlinkInput, value);
        }

        private void SetEyeOpenValue(ref float target, float value)
        {
            float clamped = Mathf.Clamp(value, EyeCloseHeight, faceDetector.CalibrationData.eyeOpenHeight);
            if (value > faceDetector.CalibrationData.eyeOpenHeight)
            {
                target = 0;
            }
            else
            {
                float range = faceDetector.CalibrationData.eyeOpenHeight - EyeCloseHeight;
                //細目すぎてrangeが負になるケースも想定してる: このときはまばたき自体無効にしておく
                if (range < Mathf.Epsilon)
                {
                    target = 0;
                }
                else
                {
                    target = 1 - (clamped - EyeCloseHeight) / range;
                }
            }
        }

        private IEnumerator DelayedSetFunBlendShape()
        {
            yield return new WaitForSeconds(0.2f);
            _blendShapeProxy?.ImmediatelySetValue(FunKey, FaceDefaultFunValue);
        }
    }
}
