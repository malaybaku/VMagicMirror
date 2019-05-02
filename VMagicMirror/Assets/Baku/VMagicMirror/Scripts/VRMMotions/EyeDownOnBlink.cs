using System;
using System.Linq;
using UnityEngine;
using UniRx;
using VRM;

namespace Baku.VMagicMirror
{
    public class EyeDownOnBlink : MonoBehaviour
    {
        //TODO: ブレンドシェイプの名前を可変にする: 下記はあくまでVRoid Studioの(v0.6.3の)出力で用いられてる名前であって仕様ではないため
        private const string EyeBrowUpBlendShapeKey = "Face.M_F00_000_00_Fcl_BRW_Surprised";
        private const string EyeBrowDownBlendShapeKey = "Face.M_F00_000_00_Fcl_BRW_Angry";

        [SerializeField]
        private float eyeBrowAmplify = 0.3f;

        [SerializeField]
        private float eyeBrowDiffSize = 0.05f;

        [SerializeField]
        private float defaultOffset = 20f;
            
        [SerializeField]
        private float eyeBrowDownOffsetWhenEyeClosed = 80f;

        [SerializeField]
        private float angleDegreeWhenEyeClosed = 10f;

        [SerializeField]
        private float speedLerpFactor = 0.2f;
        [SerializeField]
        [Range(0.05f, 1.0f)]
        private float timeScaleFactor = 0.3f;

        private VRMBlendShapeProxy _blendShapeProxy = null;
        private FaceDetector _faceDetector = null;
        private Transform _rightEyeBone = null;
        private Transform _leftEyeBone = null;

        private BlendShapeTarget _eyeBrowUpBlendShapeTarget;
        private BlendShapeTarget _eyeBrowDownBlendShapeTarget;

        private float _rightEyeBrowValue = 0.0f;
        private float _leftEyeBrowValue = 0.0f;

        //単位: %
        private float _prevEyeBrowWeight = 0;
        //単位: %/s
        private float _prevEyeBrowSpeed = 0;

        private IDisposable _rightEyeBrowHeight = null;
        private IDisposable _leftEyeBrowHeight = null;

        public bool IsInitialized { get; private set; } = false;

        public void Initialize(
            VRMBlendShapeProxy proxy,
            FaceDetector faceDetector,
            Transform rightEyeBone, 
            Transform leftEyeBone
            )
        {
            _blendShapeProxy = proxy;
            _faceDetector = faceDetector;
            _rightEyeBone = rightEyeBone;
            _leftEyeBone = leftEyeBone;

            InitializeEyeBrowBlendShapes();

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
            if (!IsInitialized || 
                _rightEyeBone == null || 
                _leftEyeBone == null
                )
            {
                return;
            }

            AdjustEyeRotation();
            AdjustEyeBrow();
        }

        private void OnDestroy()
        {
            _rightEyeBrowHeight?.Dispose();
            _rightEyeBrowHeight = null;

            _leftEyeBrowHeight?.Dispose();
            _leftEyeBrowHeight = null;
        }

        private void InitializeEyeBrowBlendShapes()
        {
            var renderers = _blendShapeProxy
                .gameObject
                .GetComponentsInChildren<SkinnedMeshRenderer>()
                .ToArray();

            _eyeBrowUpBlendShapeTarget.isValid = false;
            _eyeBrowDownBlendShapeTarget.isValid = false;

            bool _upInitialized = false;
            bool _downInitialized = false;

            for(int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                var mesh = renderer.sharedMesh;
                for (int j = 0; j < mesh.blendShapeCount; j++)
                {
                    string blendShapeName = mesh.GetBlendShapeName(j);
                    if (blendShapeName == EyeBrowUpBlendShapeKey)
                    {
                        _eyeBrowUpBlendShapeTarget = new BlendShapeTarget()
                        {
                            isValid = true,
                            index = mesh.GetBlendShapeIndex(blendShapeName),
                            renderer = renderer,
                        };
                        _upInitialized = true;
                    }

                    if (blendShapeName == EyeBrowDownBlendShapeKey)
                    {
                        _eyeBrowDownBlendShapeTarget = new BlendShapeTarget()
                        {
                            isValid = true,
                            index = mesh.GetBlendShapeIndex(blendShapeName),
                            renderer = renderer,
                        };
                        _downInitialized = true;
                    }

                    if (_upInitialized && _downInitialized)
                    {
                        return;
                    }
                }
            }
        }

        private void AdjustEyeRotation()
        {
            float leftBlink = _blendShapeProxy.GetValue(BlendShapePreset.Blink_L);
            float rightBlink = _blendShapeProxy.GetValue(BlendShapePreset.Blink_R);

            //NOTE: 毎回LookAtで値がうまく設定されてる前提でこういう記法になっている事に注意
            _leftEyeBone.localRotation *= Quaternion.AngleAxis(
                angleDegreeWhenEyeClosed * leftBlink,
                Vector3.right
                );

            _rightEyeBone.localRotation *= Quaternion.AngleAxis(
                angleDegreeWhenEyeClosed * rightBlink,
                Vector3.right
                );
        }

        private void AdjustEyeBrow()
        {
            if (_faceDetector == null)
            {
                return;
            }

            float mean = (_leftEyeBrowValue + _rightEyeBrowValue) * 0.5f;
            float meanWithOffset = mean - _faceDetector.CalibrationData.eyeBrowPosition;
            float shapeValue = Mathf.Clamp(meanWithOffset / eyeBrowDiffSize, -1, 1) * 100f * eyeBrowAmplify;

            //まばたき分の閉じ値とオフセットも追加: もともと画像処理で眉の位置を取ってるが、それをさらに強調することになる
            float goalWeight = shapeValue;
            float idealSpeed = (goalWeight - _prevEyeBrowWeight) / timeScaleFactor;
            float speed = Mathf.Lerp(_prevEyeBrowSpeed, idealSpeed, speedLerpFactor);
            float weight = _prevEyeBrowWeight + Time.deltaTime * speed;

            weight = Mathf.Clamp(weight, -100, 100);

            //weightToAssignのオフセット項は後付けの補正なので速度の計算基準に使わないよう、計算から外している
            float blink =
                (_blendShapeProxy.GetValue(BlendShapePreset.Blink_L) +
                _blendShapeProxy.GetValue(BlendShapePreset.Blink_R)
                ) * 0.5f;
            float weightToAssign = weight + defaultOffset - blink * eyeBrowDownOffsetWhenEyeClosed;

            if (_eyeBrowUpBlendShapeTarget.isValid)
            {
                _eyeBrowUpBlendShapeTarget.renderer.SetBlendShapeWeight(
                    _eyeBrowUpBlendShapeTarget.index,
                    weightToAssign > 0 ? weightToAssign : 0
                    );
            }

            if (_eyeBrowDownBlendShapeTarget.isValid)
            {
                _eyeBrowDownBlendShapeTarget.renderer.SetBlendShapeWeight(
                    _eyeBrowDownBlendShapeTarget.index,
                    weightToAssign < 0 ? -weightToAssign : 0
                    );
            }

            _prevEyeBrowWeight = weight;
            _prevEyeBrowSpeed = speed;

        }

        struct BlendShapeTarget
        {
            public bool isValid;
            public SkinnedMeshRenderer renderer;
            public int index;
        }
    }
}
