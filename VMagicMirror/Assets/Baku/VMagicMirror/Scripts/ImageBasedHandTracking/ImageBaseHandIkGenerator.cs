using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 画像処理で得た手の姿勢をキャラのIK情報にしてくれるやつ。
    /// </summary>
    /// <remarks>
    /// 手が検出できなくなったときは適当に手を下げた状態に持っていきます。
    /// </remarks>
    public class ImageBaseHandIkGenerator : MonoBehaviour
    {
        [Tooltip("ウェブカメラ上での顔と手が画面の両端にあるとしたら頭と手をどのくらいの距離にしますか、という値(m)")]
        [SerializeField] private float handPositionScale = 1.0f;
        
        [Tooltip("この時間だけ手のトラッキングが切断したばあい、検出されなくなったと判断して手を下げ始める")]
        [SerializeField] private float handNonTrackedCountDown = 0.6f;

        //NOTE: この角度は手が真正面に向く回転を指定してればOKで、最終的に定数にしてもいいです
        [SerializeField] private Vector3 rightHandForwardRotEuler = new Vector3(0, -90, 90);
        [SerializeField] private Vector3 leftHandForwardRotEuler = new Vector3(0, 90, -90);

        //手が一定時間検出されないときにIKを持ってく先で、右手のほうを指定する。左手側はXだけ反転する
        [SerializeField] private Vector3 rightHandHipOffsetWhenNotDetected = new Vector3(0.4f, 0, 0.1f);
        [SerializeField] private Vector3 rightHandDownRotEuler = new Vector3(0, 0, -80);
        [SerializeField] private Vector3 leftHandDownRotEuler = new Vector3(0, 0, 80);

        [SerializeField] private float posLerpFactor = 12f;
        [SerializeField] private float rotLerpFactor = 12f;
        [SerializeField] private float posLerpFactorNonTrack = 3f;
        [SerializeField] private float rotLerpFactorNonTrack = 3f;
        
        private FaceTracker _faceTracker = null;
        private HandTracker _handTracker = null;
        private IVRMLoadable _vrmLoadable = null;
        
        private Vector3 leftHandHipOffsetWhenNotDetected
            => new Vector3(
                -rightHandHipOffsetWhenNotDetected.x,
                rightHandHipOffsetWhenNotDetected.y,
                rightHandHipOffsetWhenNotDetected.z
               );

        [Inject]
        public void Initialize(FaceTracker faceTracker, HandTracker handTracker, IVRMLoadable vrmLoadable)
        {
            _faceTracker = faceTracker;
            _handTracker = handTracker;
            _vrmLoadable = vrmLoadable;
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;
        }

        private bool _hasVrmInfo = false;
        private Transform _head = null;
        private Transform _hips = null;

        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKGenerator LeftHand => _leftHand;

        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKGenerator RightHand => _rightHand;

        private Vector3 _rawLeftHandPos;
        private Quaternion _rawLeftHandRot;

        private Vector3 _rawRightHandPos;
        private Quaternion _rawRightHandRot;
        
        private bool _firstLeftHandDetectionDone = false;
        private bool _firstRightHandDetectionDone = false;

        private float _leftHandNonTrackCountDown = 0f;
        private float _rightHandNonTrackCountDown = 0f;

        /// <summary>
        /// 左手の手検出データが更新されるとtrueになります。値を読んだらフラグを下げることで、次に検出されるのを待つことができます。
        /// </summary>
        public bool HasLeftHandUpdate { get; set; } = false;
        
        /// <summary>
        /// 右手の手検出データが更新されるとtrueになります。値を読んだらフラグを下げることで、次に検出されるのを待つことができます。
        /// </summary>
        public bool HasRightHandUpdate { get; set; } = false;

        private void Update()
        {
            if (!_hasVrmInfo)
            {
                return;
            }
            
            //HandTrackerからのデータ吸い上げるのがこっち
            ConsumeUpdatedHandPosture();

            //IKをいい感じにするのはこっち
            UpdateIkPositions();
        }

        private void ConsumeUpdatedHandPosture()
        {
            //左なら左手、右なら右手、という感じで分けている 
            //※両手も別にやって良さそうなんでそのうち改修が入ります。
            
            if (!_handTracker.HasValidHandDetectResult)
            {
                return;
            }

            //フラグを下げることで、今入っているデータを消費する。Produce/Consumeみたいなアレですね。
            _handTracker.HasValidHandDetectResult = false;
            
            var handPos = _handTracker.HandPosition - _handTracker.ReferenceFacePosition;
            if (handPos.x > 0)
            {
                HasRightHandUpdate = true;
                _rawRightHandRot = Quaternion.Euler(rightHandForwardRotEuler);
                _rawRightHandPos =
                    _head.position +
                    new Vector3(0, 0, 0.2f) +
                    handPositionScale * new Vector3(handPos.x, handPos.y, 0);

                _rightHandNonTrackCountDown = handNonTrackedCountDown;
            }
            else
            {
                HasLeftHandUpdate = true;
                _rawLeftHandRot = Quaternion.Euler(leftHandForwardRotEuler);
                _rawLeftHandPos =
                    _head.position +
                    new Vector3(0, 0, 0.2f) +
                    handPositionScale * new Vector3(handPos.x, handPos.y, 0);

                _leftHandNonTrackCountDown = handNonTrackedCountDown;
            }
           
        }

        private void UpdateIkPositions()
        {
            if (_leftHandNonTrackCountDown > 0)
            {
                _leftHandNonTrackCountDown -= Time.deltaTime;
            }

            if (_rightHandNonTrackCountDown > 0)
            {
                _rightHandNonTrackCountDown -= Time.deltaTime;
            }

            var hipsPos = _hips.position;
            {
                if (_leftHandNonTrackCountDown > 0)
                {
                    _leftHand.Position = Vector3.Lerp(
                        _leftHand.Position, _rawLeftHandPos, Time.deltaTime * posLerpFactor);

                    _leftHand.Rotation = Quaternion.Slerp(
                        _leftHand.Rotation, _rawLeftHandRot, Time.deltaTime * rotLerpFactor);
                }
                else
                {
                    _leftHand.Position = Vector3.Lerp(
                        _leftHand.Position, 
                        hipsPos + leftHandHipOffsetWhenNotDetected,
                        Time.deltaTime * posLerpFactorNonTrack
                        );
                    _leftHand.Rotation = Quaternion.Slerp(
                        _leftHand.Rotation, 
                        Quaternion.Euler(leftHandDownRotEuler),
                        Time.deltaTime * rotLerpFactorNonTrack
                        );
                }
            }
            {
                if (_rightHandNonTrackCountDown > 0)
                {
                    _rightHand.Position = Vector3.Lerp(
                        _rightHand.Position, _rawRightHandPos, Time.deltaTime * posLerpFactor);

                    _rightHand.Rotation = Quaternion.Slerp(
                        _rightHand.Rotation, _rawRightHandRot, Time.deltaTime * rotLerpFactor);
                }
                else
                {
                    _rightHand.Position = Vector3.Lerp(
                        _rightHand.Position, 
                        hipsPos + rightHandHipOffsetWhenNotDetected, 
                        Time.deltaTime * posLerpFactorNonTrack
                        );
                    _rightHand.Rotation = Quaternion.Slerp(
                        _rightHand.Rotation, 
                        Quaternion.Euler(rightHandDownRotEuler), 
                        Time.deltaTime * rotLerpFactorNonTrack
                        );
                }
            }
        }
        
        private void OnVrmDisposing()
        {
            _hasVrmInfo = false;
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _head = info.animator.GetBoneTransform(HumanBodyBones.Head);
            _hips = info.animator.GetBoneTransform(HumanBodyBones.Hips);
            _hasVrmInfo = true;
        }

    }
}
