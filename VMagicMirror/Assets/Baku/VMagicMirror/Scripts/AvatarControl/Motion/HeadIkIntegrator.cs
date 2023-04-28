using Baku.VMagicMirror.GameInput;
using Baku.VMagicMirror.IK;
using RootMotion.FinalIK;
using UnityEngine;
using Zenject;
using Quaternion = UnityEngine.Quaternion;
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
        private readonly CameraBasedLookAtIk _camBasedLookAt = new CameraBasedLookAtIk();
        private LookAtStyles _lookAtStyle = LookAtStyles.MousePointer;
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

        private HandTargetType RightHandTargetType => handIKIntegrator.RightTargetType.Value;
        
        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            Camera mainCam,
            IKTargetTransforms ikTargets,
            PenTabletProvider penTabletProvider,
            FaceControlConfiguration faceControlConfig,
            KeyboardGameInputSource keyboardGameInputSource,
            BodyMotionModeController motionModeController
            )
        {
            _camera = mainCam.transform;
            _lookAtTarget = ikTargets.LookAt;
            _faceControlConfig = faceControlConfig;
            _penTabletProvider = penTabletProvider;
            _keyboardGameInputSource = keyboardGameInputSource;
            _motionModeController = motionModeController;

            vrmLoadable.VrmLoaded += info =>
            {
                _head = info.controlRig.GetBoneTransform(HumanBodyBones.Head);
                _lookAtIk = info.vrmRoot.GetComponentInChildren<LookAtIK>();
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
            float xPos = (x - Screen.width * 0.5f) / 1000;
            //NOTE: Yについては目が上向きになりすぎないよう強めに制限する
            float yPos = (y - Screen.height * 0.5f) / 1000;
            yPos -= 0.3f;
            if (yPos > 0)
            {
                yPos *= 0.7f;
            }

            float xClamped = Mathf.Clamp(xPos, -1f, 1f);
            float yClamped = Mathf.Clamp(yPos, -1f, 0.5f);
            
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
            float depthFactor = 1.0f;
            if (horizontalCamForward.z < 0.5f)
            {
                depthFactor = (horizontalCamForward.z - 0.1f) * 2.5f;
            }
            
            //キャラを背後から映してるハズ: 奥行き方向にLookAtをずらしていく
            var camPosition = _camera.position;
            //Vector3.Dotのとこ = カメラからみてキャラが立ってる位置の奥行き。Yを考慮すると面倒なことになるため、XZ平面でやってます
            float depth = (2 * depthFactor) * Mathf.Abs(Vector3.Dot(
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

        public void SetLookAtStyle(string content)
        {
            _lookAtStyle = LookAtStyleUtil.GetLookAtStyle(content);
        }

        private void Start()
        {
            _camBasedLookAt.Camera = _camera;
            //起動後にマウスを動かさないとLookAtの先が原点になっちゃうので、それを防ぐためにやる
            _mouseBasedLookAt.Position = _camera.position;
            _lookAtTarget.localPosition = _camBasedLookAt.Position;
        }

        //NOTE: タイミングがIKの適用前になることに注意: つまりTボーンっぽい状態
        private void Update()
        {
            _mouseActionCount = Mathf.Max(0f, _mouseActionCount - Time.deltaTime);
            _camBasedLookAt.CheckDepthAndWeight(_head);

            // - 外部トラッキングが有効ならLookAtはなくす。外部トラッキングの精度がよいので。
            // - 3人称視点でゲーム入力ベースの移動をする場合、正面付近に対してLookAtするとかえって不自然なので何もしない
            if (_hasModel && 
                (_faceControlConfig.ControlMode == FaceControlModes.ExternalTracker ||
                IsGameInputAndThirdPersonViewMode()))
            {
                //NOTE: 外部トラッキング + PenTabletのときにLookAtをやるべきかという問題があるが、無視する。
                //当面はそっちのほうが分かりやすいので
                _lookAtIk.enabled = false;
                //NOTE: 正面向きに持っていけば安全、という考え方
                _lookAtTarget.localPosition = _head.position + Vector3.forward * 5.0f;
                return;
            }
            
            if (_hasModel)
            {
                _lookAtIk.enabled = true;
            }

            var lookAtStyle = _lookAtStyle;
            //マウスがゲーム入力扱いの場合、LookAtもやると二重適用になって変なことになるため、コッチは切ってしまう
            if (_keyboardGameInputSource.MouseMoveLookAroundActive && 
                lookAtStyle == LookAtStyles.MousePointer)
            {
                lookAtStyle = LookAtStyles.Fixed;
            }
            
            var pos = 
                (lookAtStyle == LookAtStyles.MousePointer) ? _mouseBasedLookAt.Position :
                (lookAtStyle == LookAtStyles.MainCamera) ? _camBasedLookAt.Position :
                (_hasModel && lookAtStyle == LookAtStyles.Fixed) ? _head.position + Vector3.forward * 5.0f : 
                new Vector3(1, 0, 1);

            //TODO: この処理をオンオフできてもいいかも？
            if (RightHandTargetType == HandTargetType.PenTablet)
            {
                float rate = Mathf.Clamp01(_mouseActionCount / penTabletFocusCount);
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
            float distantAreaRate = (rate - .5f) * 2;
            return Vector3.Lerp(distanceLimitedResult, rawPos, distantAreaRate * distantAreaRate);
        }

        private bool IsGameInputAndThirdPersonViewMode()
        {
            return 
                _motionModeController.MotionMode.Value == BodyMotionMode.GameInputLocomotion &&
                _motionModeController.CurrentGameInputLocomotionStyle.Value != GameInputLocomotionStyle.FirstPerson;
        }
        
        class CameraBasedLookAtIk : IIKData
        {
            public Transform Camera { get; set; }

            public Vector3 Position => Vector3.Lerp(
                Camera.position + _depth * Camera.forward,
                _fixedLookAtPos,
                _fixedLookAtBlendWeight);
                
            public Quaternion Rotation => Camera.rotation;

            public void CheckDepthAndWeight(Transform head)
            {
                //Zの決め方に注意: キャラを正面から見ているときと後ろから見ているときで、手前にLookAtさせるか奥にLookAtさせるかを変更
                var forward = Camera.forward;
                var horizontalForward = new Vector3(forward.x, 0, forward.z).normalized;
            
                //zの値が小さい = カメラは真横、または後ろを向いている = キャラを正面から見ているハズ
                if (horizontalForward.z < 0.1f || head == null)
                {
                    _depth = 0;
                    _fixedLookAtBlendWeight = 0;
                    return;
                }
            
                //カメラのZ成分が増える(=真後ろから見る)のに近づくにつれて奥側を向かせる。
                //このとき、途中のブレンディングをするとき正面向き成分を混ぜることで、遷移中のLookAtを体にめり込みにくくする
                float depthFactor = 1.0f;
                if (horizontalForward.z < 0.5f)
                {
                    depthFactor = horizontalForward.z * 2;
                }

                //キャラを背後から映してるハズ: 奥行き方向にLookAtをずらしていく
                var camPosition = Camera.position;
                //Vector3.Dotのとこ = カメラからみてキャラが立ってる位置の奥行き。Yを考慮すると面倒なことになるため、XZ平面でやってます
                _depth = (2 * depthFactor) * Mathf.Abs(Vector3.Dot(
                                  new Vector3(camPosition.x, 0, camPosition.z), horizontalForward
                              ));

                
                //固定視点LookAtのウェイト (いわゆるテント写像)
                //depthFactor == 1つまりキャラの中心付近を通るときに1になって固定視点を経由し、両端(=通常のケース)ではゼロになる
                _fixedLookAtPos = head.position + head.forward * 1.0f;
                _fixedLookAtBlendWeight = 1.0f - 2.0f * Mathf.Abs(depthFactor - 0.5f);
            }

            //カメラが前方向き = キャラを後ろから映しているときの見栄えが破綻しないように奥行きを追加するやつ
            private float _depth = 0f;
            private Vector3 _fixedLookAtPos = Vector3.zero;
            private float _fixedLookAtBlendWeight = 0;

            public IKTargets Target => IKTargets.HeadLookAt;
        }
    }
}
