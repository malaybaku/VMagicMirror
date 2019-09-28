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
        [SerializeField] private Transform rightHand = null;
        [SerializeField] private Transform leftHand = null;

        private Vector3 _gamePadCenterInitialPosition;
        
        private void Start()
        {
            _gamePadCenterInitialPosition = gamepadCenter.localPosition;
        }
        
        /// <summary>
        /// [-1, 1]の範囲で表現されたスティック情報をもとにゲームパッドの位置を変更します。
        /// </summary>
        /// <param name="pos"></param>
        public void SetHorizontalPosition(Vector2 pos)
        {
            gamepadCenter.localPosition =
                _gamePadCenterInitialPosition +
                new Vector3(
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
        
        public void SetHeight(float height) => transform.position = height * Vector3.up;
        
        //NOTE: yのスケールも必ず変える: 傾けたときにローカル座標が歪まないようにするため(いわゆるアフィン変換っぽいのをやりたい)
        public void SetHorizontalScale(float scale) => transform.localScale = scale * Vector3.one;
        
        //ワールドのを渡す点に注意
        public (Vector3, Quaternion) GetRightHand() => (rightHand.position, rightHand.rotation);
        public (Vector3, Quaternion) GetLeftHand() => (leftHand.position, leftHand.rotation);

    }
}
