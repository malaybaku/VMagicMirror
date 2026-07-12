using Baku.VMagicMirror.GameInput;
using Baku.VMagicMirror.IK;
using RootMotion.FinalIK;
using UnityEngine;
using Zenject;
using Vector3 = UnityEngine.Vector3;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 頭のIKというかLookAtIk的なところを制御するやつ
    /// </summary>
    public class HeadIkIntegrator : MonoBehaviour
    {
        //手のIKよりLookAtのIKをやや前方にずらして見栄えを調整する決め打ちのパラメータ
        private const float ZOffsetOnHeadIk = 0.6f;

        private const float PenTabletLookAtProximalDistance = 0.3f;

        [SerializeField] private HandIKIntegrator handIKIntegrator = null;
        [SerializeField] private float lookAtSpeedFactor = 6.0f;
        [SerializeField] private float mouseActionCountMax = 8.0f;
        [SerializeField] private float penTabletFocusCount = 3.0f;
        //MouseMoveが呼ばれたらTime.deltaTimeにこの倍率をかけてカウントを増やす
        [SerializeField] private float mouseMoveIncrementFactor = 4.0f;
        //MouseButtonがDownで呼ばれたら、この値そのままでカウントを増やす
        [SerializeField] private float mouseClickIncrementValue = 2.0f;

        private readonly IKDataRecord _mouseBasedLookAt = new IKDataRecord();
        private Transform _head = null;
        private bool _hasModel = false;

        private Transform _camera = null;
        private Transform _lookAtTarget = null;
        private FaceControlConfiguration _faceControlConfig;
        private PenTabletProvider _penTabletProvider;
        private KeyboardGameInputSource _keyboardGameInputSource;
        private BodyMotionModeController _motionModeController;

        private LookAtIK _lookAtIk = null;

        private float _mouseActionCount = 0f;

        private HandTargetType RightHandTargetType => handIKIntegrator.RightTargetType.CurrentValue;
        
        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            [Inject(Id = "RefCameraForRay")] Camera cam,
            IKTargetTransforms ikTargets,
            PenTabletProvider penTabletProvider,
            FaceControlConfiguration faceControlConfig,
            KeyboardGameInputSource keyboardGameInputSource,
            BodyMotionModeController motionModeController
            )
        {
            _camera = cam.transform;
            _lookAtTarget = ikTargets.LookAt;
            _faceControlConfig = faceControlConfig;
            _penTabletProvider = penTabletProvider;
            _keyboardGameInputSource = keyboardGameInputSource;
            _motionModeController = motionModeController;

            vrmLoadable.VrmLoaded += info =>
            {
                _head = info.AvatarBones.Head;
                _lookAtIk = info.VrmRoot.GetComponentInChildren<LookAtIK>();
                _hasModel = true;
            };
            vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _lookAtIk = null;
                _head = null;
            };
        }

        public void OnMouseMove(int x, int y)
        {
            _mouseActionCount = Mathf.Min(mouseActionCountMax, _mouseActionCount + Time.deltaTime * mouseMoveIncrementFactor);

            //画面中央 = カメラ位置なのでコレで空間的にだいたい正しいハズ
            var xPos = (x - Screen.width * 0.5f) / 1000;
            //NOTE: Yについては目が上向きになりすぎないよう強めに制限する
            var yPos = (y - Screen.height * 0.5f) / 1000;
            yPos -= 0.3f;
            if (yPos > 0)
            {
                yPos *= 0.7f;
            }

            var xClamped = Mathf.Clamp(xPos, -1f, 1f);
            var yClamped = Mathf.Clamp(yPos, -1f, 0.5f);
            
            var baseLookAtPosition =
                _camera.TransformPoint(xClamped, yClamped, 0) + 
                ZOffsetOnHeadIk * Vector3.forward;

            //Zの決め方に注意: キャラを正面から見ているときと後ろから見ているときで、手前にLookAtさせるか奥にLookAtさせるかを変更
            var camForward = _camera.forward;
            var horizontalCamForward = new Vector3(camForward.x, 0, camForward.z).normalized;
            
            //zの値が小さい = カメラは真横、または後ろを向いている = キャラを正面から見ているハズ
            if (horizontalCamForward.z < 0.1f)
            {
                _mouseBasedLookAt.Position = baseLookAtPosition;
                return;
            }
            
            //カメラのZ成分が増える(=真後ろから見る)のに近づくにつれて奥側を向かせるようにする
            var depthFactor = 1.0f;
            if (horizontalCamForward.z < 0.5f)
            {
                depthFactor = (horizontalCamForward.z - 0.1f) * 2.5f;
            }
            
            //キャラを背後から映してるハズ: 奥行き方向にLookAtをずらしていく
            var camPosition = _camera.position;
            //Vector3.Dotのとこ = カメラからみてキャラが立ってる位置の奥行き。Yを考慮すると面倒なことになるため、XZ平面でやってます
            var depth = (2 * depthFactor) * Mathf.Abs(Vector3.Dot(
                new Vector3(camPosition.x, 0, camPosition.z), horizontalCamForward
                ));
            _mouseBasedLookAt.Position = baseLookAtPosition + depth * camForward;
        }

        public void OnMouseButton(string eventName)
        {
            if (eventName == MouseButtonEventNames.LDown ||
                eventName == MouseButtonEventNames.RDown ||
                eventName == MouseButtonEventNames.MDown)
            {
                _mouseActionCount = Mathf.Min(mouseActionCountMax, _mouseActionCount + mouseClickIncrementValue);
            }
        }

        private void Start()
        {
            //起動後にマウスを動かさないとLookAtの先が原点になっちゃうので、それを防ぐためにやる
            _mouseBasedLookAt.Position = _camera.position;
            _lookAtTarget.localPosition = _mouseBasedLookAt.Position;
        }

        //NOTE: タイミングがIKの適用前になることに注意
        private void Update()
        {
            _mouseActionCount = Mathf.Max(0f, _mouseActionCount - Time.deltaTime);

            if (_hasModel && ShouldDisableLookAtIk())
            {
                DisableLookAtIk();
                return;
            }
            
            if (_hasModel)
            {
                _lookAtIk.enabled = true;
            }

            var pos = _mouseBasedLookAt.Position;

            //TODO: この処理をオンオフできてもいいかも？
            if (RightHandTargetType == HandTargetType.PenTablet)
            {
                var rate = Mathf.Clamp01(_mouseActionCount / penTabletFocusCount);
                pos = CreatePenTabletLookAt(pos, _penTabletProvider.GetPosFromScreenPoint(), rate);
            }
            
            _lookAtTarget.localPosition = Vector3.Lerp(
                _lookAtTarget.localPosition,
                pos,
                lookAtSpeedFactor * Time.deltaTime
            );
        }

        private Vector3 CreatePenTabletLookAt(Vector3 rawPos, Vector3 penTabletPos, float rate)
        {
            //NOTE: rawPosはすごく遠い位置を指定しがちなのでLerpだとうまく行かない。

            rate = 1 - rate;

            //rateが0 - 0.5の間では、ペンタブ付近の決まった距離のなかにLookAtを収める
            var distanceLimitedResult = penTabletPos + 
                    (rawPos - penTabletPos).normalized * (PenTabletLookAtProximalDistance * Mathf.Clamp01(rate * 2f));
            
            if (rate < 0.5f)
            {
                return distanceLimitedResult;
            }
            
            //ペンタブからある程度離れたら二次カーブでLerpする。こうすると0.5f付近で繋がるし、問題も起きにくい…はず 
            var distantAreaRate = (rate - .5f) * 2;
            return Vector3.Lerp(distanceLimitedResult, rawPos, distantAreaRate * distantAreaRate);
        }

        private bool IsGameInputAndThirdPersonViewMode()
        {
            return 
                _motionModeController.MotionMode.CurrentValue == BodyMotionMode.GameInputLocomotion &&
                _motionModeController.CurrentGameInputLocomotionStyle.CurrentValue != GameInputLocomotionStyle.FirstPerson;
        }

        private void DisableLookAtIk()
        {
            _lookAtIk.enabled = false;
            //NOTE: 正面向きに持っていけば安全、という考え方
            _lookAtTarget.localPosition = _head.position + Vector3.forward * 5.0f;
        }

        /// <summary>
        /// LookAtを切るかどうかを判定する。
        /// - 外部トラッキングやVMCProtocolでの動作中はそっちの動作を厳密に適用したいはずと想定して切る
        /// - そうでない場合は、LookAtMode + ゲーム入力が適用中かどうかを考慮して切る
        /// </summary>
        /// <returns></returns>
        private bool ShouldDisableLookAtIk()
        {
            if (_faceControlConfig.WebCamMouseLookAtModeValue is WebCamMouseLookAtModes.Never)
            {
                return true;
            }

            if (IsGameInputAndThirdPersonViewMode())
            {
                return true;
            }

            // マウス移動で頭部動作するモードの場合、LookAtが混ざると見栄えが悪いので切る
            if (_keyboardGameInputSource.MouseMoveLookAroundActive)
            {
                return true;
            }

            // NOTE: 外部トラッキング + PenTablet等のときには実はLookAtしていい説もあるが、分岐のシンプルさを優先してやっていない。
            // 「外部トラッキング + マウス注視」もできてOK、という思想に乗り換えたらこの辺の分岐を見直すかも
            return _faceControlConfig.HeadMotionControlModeValue switch
            {
                FaceControlModes.WebCam =>
                    _faceControlConfig.WebCamMouseLookAtModeValue is not WebCamMouseLookAtModes.Always,
                FaceControlModes.ExternalTracker or FaceControlModes.VMCProtocol => true,
                _ => false,
            };
        }
    }
}
