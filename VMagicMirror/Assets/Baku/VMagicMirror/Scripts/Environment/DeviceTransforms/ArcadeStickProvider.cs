using System.Collections.Generic;
using mattatz.TransformControl;
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

        //ボタンのちょっと上に腕があったほうがいいよね、という補正値
        //スティックと少しだけズラしておくと見栄えがよい
        [SerializeField] private float buttonYOffset = 0.04f;
        
        //スティックの付け根からボール部分の中心までの高さ、のつもり
        [SerializeField] private float stickHeight = 0.05f;
        //スティックが特定方向に最大まで倒れたときの倒れ込み角度。これとstickHeightを使うと手の位置、姿勢が定まる
        [SerializeField] private float stickBendAngleDeg = 20f;
        //スティックを横に最大限まで倒したときに手首をひねる角度
        [SerializeField] private float handTiltDeg = 5f;

        [SerializeField] private Vector3 basePosition = new Vector3(0f, .93f, 0.3f);
        [SerializeField] private Vector3 baseRotation = new Vector3(-20f, 0f, 0f);

        [SerializeField] private TransformControl transformControl = default;
        public TransformControl TransformControl => transformControl;

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

            var rot = baseRot * 
                  Quaternion.AngleAxis(inputValue.x * handTiltDeg, Vector3.forward) * 
                  //NOTE: ちょっとだけ手首が上向きになるように仕向ける。IKの計算上あんまり見栄えは改善しないが、無いよりはgood
                  Quaternion.AngleAxis(-10f, Vector3.right) * 
                  Quaternion.AngleAxis(90f, Vector3.up);

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
                Debug.Log($"unsupported key! {key}");
                //NOTE: ホントはここに来られると困る
                return (aButton.position, aButton.rotation);
            }

            //NOTE: 外側のボタンに対して手首のひねりを効かせることが考えられる…が、
            //とりあえずコード上ではそういう事はしないものとする
            var t = _keyToTransform[key];
            return (t.position + t.up * buttonYOffset, t.rotation * Quaternion.AngleAxis(-90f, Vector3.up));
        }
        
        /// <summary>
        /// 手のための位置や回転オフセットを考慮しない、ボタン自体の姿勢情報を取得します。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public (Vector3, Quaternion) GetRightHandRaw(GamepadKey key)
        {
            if (!IsArcadeStickKey(key))
            {
                return (aButton.position, aButton.rotation);
            }

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
        /// スティック部分にまっすぐ手を添えに行く場合の左手首のrotationを取得します。
        /// 手のサイズベースで位置オフセットをつけたい場合などに使用します。
        /// </summary>
        /// <returns></returns>
        public Quaternion GetStickBaseRotation()
        {
            return stickBase.rotation * Quaternion.AngleAxis(90f, Vector3.up);;
        }

        /// <summary>
        /// アーケードスティック全体の上方向に相当するベクトルを取得します。
        /// ボタンの押下/押上に対応して手をちょっと動かすのに使えます。
        /// </summary>
        /// <returns></returns>
        public Vector3 GetYAxis() => aButton.up;

        /// <summary>
        /// レイアウトパラメータを指定して呼ぶことで、レイアウトのリセットを行います。
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
            t.localScale = Vector3.one;
        }
    }
}
