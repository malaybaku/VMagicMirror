using Baku.VMagicMirror.Installer;
using mattatz.TransformControl;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 小さく、手で持って動かせるゲームパッドの位置と回転を表現するためのクラス
    /// </summary>
    public class GamepadProvider : MonoBehaviour
    {
        //このx, zはスケーリングの影響を受けて、スケールが小さいと動きも小さくなる
        [Tooltip("スティックを限界まで倒したときにゲームパッドが並進すべき距離(x, z)。")]
        [SerializeField] private Vector2 moveRange = new Vector2(0.05f, 0.05f);
         
        [Tooltip("スティックを限界まで倒したときにゲームパッドが傾くべき角度")]
        [SerializeField] private Vector2 posToEulerAngle = new Vector2(20f,20f);
        
        [SerializeField] private Transform gamepadCenter = null;
        [SerializeField] private Transform modelRoot = null;
        [SerializeField] private Transform rightHand = null;
        [SerializeField] private Transform leftHand = null; 
        
        [SerializeField] private Vector3 basePosition = new Vector3(0f, 1f, 0.24f);
        [SerializeField] private Vector3 baseRotation = new Vector3(-15, 0, 0);
        [SerializeField] private float baseScale = 0.8f;

        [SerializeField] private MeshRenderer bodyRenderer = null;
        
        [SerializeField] private TransformControl transformControl = null;
        public TransformControl TransformControl => transformControl;

        [SerializeField] private Transform modelScaleTarget = null;
        public Transform ModelScaleTarget => modelScaleTarget;

        private GamepadVisibilityView _visibilityView = null;
        public GamepadVisibilityView GetVisibilityView()
        {
            if (_visibilityView == null)
            {
                _visibilityView = GetComponent<GamepadVisibilityView>();
            }
            return _visibilityView;
        }
        
        // NOTE: ゲームパッドは動きが追加で入っているので、GamepadProvider.transformを返すとビジュアル的にちょっとズレた位置になってしまう
        public Pose GetModelRootPose() => new(modelRoot.position, modelRoot.rotation); 

        [Inject]
        public void Initialize(IDevicesRoot parent) => transform.parent = parent.Transform;

        private void Start()
        {
            //NOTE: 現在のゲームパッドモデルはテクスチャをただ一枚だけ持つ
            bodyRenderer.material = HIDMaterialUtil.Instance.GetGamepadBodyMaterial();
        }
        
        /// <summary>
        /// リセット処理などで明示的に呼ばれた場合、指定されたパラメタベースでゲームパッドの位置を初期化します。
        /// </summary>
        /// <param name="parameters"></param>
        public void SetLayoutByParameter(DeviceLayoutAutoAdjustParameters parameters)
        {
            var t = transform;
            t.localRotation = Quaternion.Euler(baseRotation);
            t.localPosition = new Vector3(
                basePosition.x * parameters.ArmLengthFactor,
                basePosition.y * parameters.HeightFactor,
                basePosition.z * parameters.ArmLengthFactor
                );
            t.localScale = (baseScale * parameters.ArmLengthFactor) * Vector3.one;
        }

        /// <summary>
        /// このゲームパッドを持っているはずの両手の位置の中点、およびスティックの傾きを表す値を指定することで、
        /// コントローラの望ましい位置、姿勢を生成します。
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="posOffset"></param>
        /// <param name="rotOffset"></param>
        public void SetFilteredPositionAndRotation(Vector2 pos, Vector3 posOffset, Quaternion rotOffset)
        {
            //NOTE: 2種類のTransformを使ってるのはローカルスケールの適用をやる関係で両者を分けておきたいから。
            //…なんだけど
            var localPos =new Vector3(moveRange.x * pos.x, 0.0f, moveRange.y * pos.y);
            var localRot = rotOffset * 
                Quaternion.Euler(pos.y * posToEulerAngle.y, 0, -pos.x * posToEulerAngle.x);

            gamepadCenter.localPosition = localPos;
            gamepadCenter.position += posOffset;
            gamepadCenter.localRotation = localRot;

            modelRoot.localPosition = localPos;
            modelRoot.position += posOffset;
            modelRoot.localRotation = localRot;
        }
        
        //ワールドのを渡す点に注意
        public (Vector3, Quaternion) GetRightHand() => (rightHand.position, rightHand.rotation);
        public (Vector3, Quaternion) GetLeftHand() => (leftHand.position, leftHand.rotation);

        public static ReactedHand GetPreferredReactionHand(GamepadKey key)
        {
            switch (key)
            {
                case GamepadKey.B:
                case GamepadKey.A:
                case GamepadKey.X:
                case GamepadKey.Y:
                case GamepadKey.RShoulder:
                case GamepadKey.RTrigger:
                    return ReactedHand.Right;
                case GamepadKey.UP:
                case GamepadKey.RIGHT:
                case GamepadKey.DOWN:
                case GamepadKey.LEFT:
                case GamepadKey.LShoulder:
                case GamepadKey.LTrigger:
                    return ReactedHand.Left;
                default:
                    return ReactedHand.None;
            }
        }

        public static bool IsSideKey(GamepadKey key)
        {
            switch (key)
            {
                case GamepadKey.RShoulder:
                case GamepadKey.RTrigger:
                case GamepadKey.LShoulder:
                case GamepadKey.LTrigger:
                    return true;
                default:
                    return false;
            }
        }

    }
}
