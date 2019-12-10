using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 小さく、手で持って動かせるゲームパッドの位置と回転を表現するためのクラス
    /// </summary>
    public class SmallGamepadProvider : MonoBehaviour
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
        /// [-1, 1]の範囲で表現されたスティック情報をもとにゲームパッドの位置を変更します。
        /// </summary>
        /// <param name="pos"></param>
        public void SetHorizontalPosition(Vector2 pos)
        {
            gamepadCenter.localPosition = new Vector3(
                moveRange.x * pos.x,
                0.0f,
                moveRange.y * pos.y
                );
            
            //並進に合わせて回転もする
            gamepadCenter.localRotation = Quaternion.Euler(
                pos.y * posToEulerAngle.y,
                0,
                -pos.x * posToEulerAngle.x
                );
        }

        /// <summary>
        /// このゲームパッドを持っているはずの両手の位置の中点、およびスティックの傾きを表す値を指定することで、
        /// コントローラの望ましい位置、姿勢を生成します。
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="offset"></param>
        public void SetFilteredPosition(Vector2 pos, Vector3 offset)
        {
            modelRoot.localPosition = new Vector3(moveRange.x * pos.x, 0.0f, moveRange.y * pos.y);
            modelRoot.position += offset;
//            modelRoot.position += offsetY * Vector3.up;
            
            //並進に合わせて回転もする
            modelRoot.localRotation = Quaternion.Euler(
                pos.y * posToEulerAngle.y,
                0,
                -pos.x * posToEulerAngle.x
            );
            
        }
        
        //ワールドのを渡す点に注意
        public (Vector3, Quaternion) GetRightHand() => (rightHand.position, rightHand.rotation);
        public (Vector3, Quaternion) GetLeftHand() => (leftHand.position, leftHand.rotation);

    }
}
