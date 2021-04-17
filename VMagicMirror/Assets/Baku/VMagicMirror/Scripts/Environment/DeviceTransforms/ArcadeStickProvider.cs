using System;
using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary> アケコンのスティックやボタンの位置情報を提供するやつ </summary>
    public class ArcadeStickProvider : MonoBehaviour
    {
        //NOTE: スティック先端ではなくスティックの付け根を指定する。つまりボタン群とだいたい同じ平面上。
        [SerializeField] private Transform stickBase = default;

        [SerializeField] private Transform aButton = default;
        [SerializeField] private Transform bButton = default;
        [SerializeField] private Transform xButton = default;
        [SerializeField] private Transform yButton = default;
        //NOTE: 念の為書いておくが、R1/L1がゲームパッドの人指相当のほう
        [SerializeField] private Transform right1Button = default;
        [SerializeField] private Transform right2Button = default;
        [SerializeField] private Transform left1Button = default;
        [SerializeField] private Transform left2Button = default;

        //スティックの付け根からボール部分の中心までの高さ、のつもり
        [SerializeField] private float stickHeight = 0.08f;
        //スティックが特定方向に最大まで倒れたときの倒れ込み角度。これとstickHeightを使うと手の位置、姿勢が定まる
        [SerializeField] private float stickBendAngleDeg = 30f;
        //スティックを横に最大限まで倒したときに手首をひねる角度
        [SerializeField] private float handTiltDeg = 5f;

        private void Awake()
        {
            _keyToTransform[GamepadKey.A] = aButton;
            _keyToTransform[GamepadKey.B] = bButton;
            _keyToTransform[GamepadKey.X] = xButton;
            _keyToTransform[GamepadKey.Y] = yButton;
            _keyToTransform[GamepadKey.RShoulder] = right1Button;
            _keyToTransform[GamepadKey.RTrigger] = right2Button;
            _keyToTransform[GamepadKey.LShoulder] = left1Button;
            _keyToTransform[GamepadKey.LTrigger] = left2Button;
        }

        private readonly Dictionary<GamepadKey, Transform> _keyToTransform = new Dictionary<GamepadKey, Transform>();
        
        //NOTE: 以下2つで手首-手先オフセットを考慮してませんが、
        //これはアケコン自体を表示しない見込みであるため、一定値だけズレるのは無害だろうという判断です

        /// <summary>
        /// 左十字ボタン、または左スティックの値を指定することで、
        /// その入力に対応したアケコンのレバー状態を考えた位置や手のあるべき角度を計算します。
        /// </summary>
        /// <param name="inputValue"></param>
        /// <returns></returns>
        public (Vector3, Quaternion) GetLeftHand(Vector2 inputValue)
        {
            //NOTE: 見栄えと実際の動きの予想も考慮して、次のように回転を使い分ける
            // - スティックx方向: 手首のひねりに少しだけ反映
            // - スティックy方向: 手が並行移動するだけで前後のひねりは無し 
            
            var basePosition = stickBase.position;
            var baseRot = stickBase.rotation;
            
            //NOTE: ちゃんとスティックが倒れるぶんを回転計算で考えると、y方向のズレが物理的に正しくなる。はず
            var positionDiff = 
                baseRot * 
                Quaternion.Euler(stickBendAngleDeg * inputValue.y, 0f, stickBendAngleDeg * inputValue.x) *
                (stickBase.up * stickHeight);

            var rot = baseRot * Quaternion.AngleAxis(inputValue.x * handTiltDeg, Vector3.forward);

            return (basePosition + positionDiff, rot);
        }
        
        /// <summary>
        /// 最後に押したキーを指定して呼び出すことで、そのボタンを押すときに手がありそうな位置を返します。
        /// </summary>
        /// <returns></returns>
        public (Vector3, Quaternion) GetRightHand(GamepadKey key)
        {
            if (!IsArcadeStickKey(key))
            {
                //NOTE: ホントはここに来られると困る
                return (aButton.position, aButton.rotation);
            }

            //NOTE: 外側のボタンに対して手首のひねりを効かせることが考えられる…が、
            //とりあえずコード上ではそういう事はしないものとする
            var t = _keyToTransform[key];
            return (t.position, t.rotation);
        }
     
        public static bool IsArcadeStickKey(GamepadKey key)
        {
            switch (key)
            {
                case GamepadKey.A:
                case GamepadKey.B:
                case GamepadKey.X:
                case GamepadKey.Y:
                case GamepadKey.RShoulder:
                case GamepadKey.LShoulder:
                case GamepadKey.RTrigger:
                case GamepadKey.LTrigger:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// アーケードスティック全体の上方向に相当するベクトルを取得します。
        /// ボタンの押下/押上に対応して手をちょっと動かすのに使えます。
        /// </summary>
        /// <returns></returns>
        public Vector3 GetYAxis() => aButton.up;
    }
}
