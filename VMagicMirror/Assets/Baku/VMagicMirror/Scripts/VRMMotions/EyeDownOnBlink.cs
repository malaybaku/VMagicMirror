using System;
using UnityEngine;
using UniRx;
using VRM;

namespace Baku.VMagicMirror
{
    public class EyeDownOnBlink : MonoBehaviour
    {
        [SerializeField]
        private float eyeBrowDiffSize = 0.05f;

        //ちょっとデフォルトで眉を上げとこう的な値。目の全開きは珍しいという仮説による。
        [SerializeField]
        private float defaultOffset = 0.2f;
            
        [SerializeField]
        private float eyeBrowDownOffsetWhenEyeClosed = 0.8f;

        [SerializeField]
        private float eyeAngleDegreeWhenEyeClosed = 10f;

        [SerializeField]
        private float speedLerpFactor = 0.2f;
        [SerializeField]
        [Range(0.05f, 1.0f)]
        private float timeScaleFactor = 0.3f;

        private VRMBlendShapeProxy _blendShapeProxy = null;
        private EyebrowBlendShapeSet _blendShapeSet = null;
        private FaceDetector _faceDetector = null;
        private Transform _rightEyeBone = null;
        private Transform _leftEyeBone = null;

        //private BlendShapeTarget _eyeBrowUpBlendShapeTarget;
        //private BlendShapeTarget _eyeBrowDownBlendShapeTarget;

        private float _rightEyeBrowValue = 0.0f;
        private float _leftEyeBrowValue = 0.0f;

        //単位: %
        private float _prevLeftEyeBrowWeight = 0;
        private float _prevRightEyeBrowWeight = 0;
        //単位: %/s
        private float _prevLeftEyeBrowSpeed = 0;
        private float _prevRightEyeBrowSpeed = 0;

        private IDisposable _rightEyeBrowHeight = null;
        private IDisposable _leftEyeBrowHeight = null;

        public bool IsInitialized { get; private set; } = false;

        public void Initialize(
            VRMBlendShapeProxy proxy,
            FaceDetector faceDetector,
            EyebrowBlendShapeSet blendShapeSet,
            Transform rightEyeBone, 
            Transform leftEyeBone
            )
        {
            _blendShapeProxy = proxy;
            _faceDetector = faceDetector;
            _blendShapeSet = blendShapeSet;
            _rightEyeBone = rightEyeBone;
            _leftEyeBone = leftEyeBone;

            //InitializeEyeBrowBlendShapes();

            _rightEyeBrowHeight?.Dispose();
            _leftEyeBrowHeight?.Dispose();

            _rightEyeBrowHeight = faceDetector.FaceParts.RightEyebrow.Height.Subscribe(
                v => _rightEyeBrowValue = v
                );

            _leftEyeBrowHeight = faceDetector.FaceParts.LeftEyebrow.Height.Subscribe(
                v => _leftEyeBrowValue = v
                );

            IsInitialized = true;
        }

        private void LateUpdate()
        {
            if (!IsInitialized)
            {
                return;
            }

            AdjustEyeRotation();
            AdjustEyebrow();
        }

        private void OnDestroy()
        {
            _rightEyeBrowHeight?.Dispose();
            _rightEyeBrowHeight = null;

            _leftEyeBrowHeight?.Dispose();
            _leftEyeBrowHeight = null;
        }

        //private void InitializeEyeBrowBlendShapes()
        //{
        //    var renderers = _blendShapeProxy
        //        .gameObject
        //        .GetComponentsInChildren<SkinnedMeshRenderer>()
        //        .ToArray();

        //    _eyeBrowUpBlendShapeTarget.isValid = false;
        //    _eyeBrowDownBlendShapeTarget.isValid = false;

        //    bool _upInitialized = false;
        //    bool _downInitialized = false;

        //    for(int i = 0; i < renderers.Length; i++)
        //    {
        //        var renderer = renderers[i];
        //        var mesh = renderer.sharedMesh;
        //        for (int j = 0; j < mesh.blendShapeCount; j++)
        //        {
        //            string blendShapeName = mesh.GetBlendShapeName(j);
        //            if (blendShapeName == EyeBrowUpBlendShapeKey)
        //            {
        //                _eyeBrowUpBlendShapeTarget = new BlendShapeTarget()
        //                {
        //                    isValid = true,
        //                    index = mesh.GetBlendShapeIndex(blendShapeName),
        //                    renderer = renderer,
        //                };
        //                _upInitialized = true;
        //            }

        //            if (blendShapeName == EyeBrowDownBlendShapeKey)
        //            {
        //                _eyeBrowDownBlendShapeTarget = new BlendShapeTarget()
        //                {
        //                    isValid = true,
        //                    index = mesh.GetBlendShapeIndex(blendShapeName),
        //                    renderer = renderer,
        //                };
        //                _downInitialized = true;
        //            }

        //            if (_upInitialized && _downInitialized)
        //            {
        //                return;
        //            }
        //        }
        //    }
        //}

        private void AdjustEyeRotation()
        {
            if (_rightEyeBone == null && _leftEyeBone == null)
            {
                return;
            }

            float leftBlink = _blendShapeProxy.GetValue(BlendShapePreset.Blink_L);
            float rightBlink = _blendShapeProxy.GetValue(BlendShapePreset.Blink_R);

            //NOTE: 毎回LookAtで値がうまく設定されてる前提でこういう記法になっている事に注意
            _leftEyeBone.localRotation *= Quaternion.AngleAxis(
                eyeAngleDegreeWhenEyeClosed * leftBlink,
                Vector3.right
                );

            _rightEyeBone.localRotation *= Quaternion.AngleAxis(
                eyeAngleDegreeWhenEyeClosed * rightBlink,
                Vector3.right
                );
        }

        private void AdjustEyebrow()
        {
            if (_faceDetector == null)
            {
                return;
            }

            //NOTE: ここスケールファクタないと非常に小さい値しか入らないのでは？？？
            float left = _leftEyeBrowValue - _faceDetector.CalibrationData.eyeBrowPosition;
            float right = _rightEyeBrowValue - _faceDetector.CalibrationData.eyeBrowPosition;


            float goalLeft = left;
            float idealLeft = (goalLeft - _prevLeftEyeBrowWeight) / timeScaleFactor;
            float speedLeft = Mathf.Lerp(_prevLeftEyeBrowSpeed, idealLeft, speedLerpFactor);
            float weightLeft = _prevLeftEyeBrowWeight + Time.deltaTime * speedLeft;
            weightLeft = Mathf.Clamp(weightLeft, -1, 1);

            float goalRight = right;
            float idealRight = (goalRight - _prevRightEyeBrowWeight) / timeScaleFactor;
            float speedRight = Mathf.Lerp(_prevRightEyeBrowSpeed, idealRight, speedLerpFactor);
            float weightRight = _prevRightEyeBrowWeight + Time.deltaTime * speedRight;
            weightRight = Mathf.Clamp(weightRight, -1, 1);

            //まばたき量に応じた値も足す: こちらはまばたき側の計算時にすでにローパスされてるから、そのまま足してOK
            //weightToAssignのオフセット項は後付けの補正なので速度の計算基準に使わないよう、計算から外している
            float blinkLeft = _blendShapeProxy.GetValue(BlendShapePreset.Blink_L);
            float weightLeftToAssign = weightLeft + defaultOffset - blinkLeft * eyeBrowDownOffsetWhenEyeClosed;

            float blinkRight = _blendShapeProxy.GetValue(BlendShapePreset.Blink_R);
            float weightRightToAssign = weightRight + defaultOffset - blinkRight * eyeBrowDownOffsetWhenEyeClosed;

            _blendShapeSet.UpdateEyebrowBlendShape(weightLeftToAssign, weightRightToAssign);

            _prevLeftEyeBrowWeight = weightLeft;
            _prevLeftEyeBrowSpeed = speedLeft;
            _prevRightEyeBrowWeight = weightRight;
            _prevRightEyeBrowSpeed = speedRight;
        }

        struct BlendShapeTarget
        {
            public bool isValid;
            public SkinnedMeshRenderer renderer;
            public int index;
        }
    }
}
