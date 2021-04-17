using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// ゲームパッドの入力状況に対してアーケードスティック型の腕IKを生成するやつ。
    /// </summary>
    public class ArcadeStickHandIKGenerator : HandIkGeneratorBase
    {
        // 左手の追従速度。かなりスピーディ
        private const float RightHandSpeedFactor = 12.0f;
        // 左手の追従速度。そこそこスピーディ
        private const float LeftHandSpeedFactor = 12.0f;
        
        //ボタンを押すとき手ごと下に下げる移動量。
        private const float ButtonDownY = 0.01f;

        public ArcadeStickHandIKGenerator(
            MonoBehaviour coroutineResponder, 
            IVRMLoadable vrmLoadable,
            ArcadeStickProvider stickProvider) : base(coroutineResponder)
        {
            _stickProvider = stickProvider;

            //モデルロード時、身長を参照することで「コントローラの移動オフセットはこんくらいだよね」を初期化
            vrmLoadable.VrmLoaded += info =>
            {
                var h = info.animator.GetBoneTransform(HumanBodyBones.Head);
                var f = info.animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            };
        }
        
        private readonly ArcadeStickProvider _stickProvider;
        
        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKGenerator LeftHand => _leftHand;

        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKGenerator RightHand => _rightHand;
        
        private Vector2 _rawStickPos = Vector2.zero;
        private Vector2 _filterStickPos = Vector2.zero;
        
        private float _offsetY = 0;
        private int _buttonDownCount = 0;

        private Vector3 _latestButtonPos;
        private Quaternion _latestButtonRot;
        
        public void ButtonDown(GamepadKey key)
        {
            if (!ArcadeStickProvider.IsArcadeStickKey(key))
            {
                return;
            }

            _buttonDownCount++;

            (_latestButtonPos, _latestButtonRot) = _stickProvider.GetRightHand(key);
        }

        public void ButtonUp(GamepadKey key)
        {
            if (!ArcadeStickProvider.IsArcadeStickKey(key))
            {
                return;
            }
            _buttonDownCount--;

            //通常起きないハズだが一応
            if (_buttonDownCount < 0)
            {
                _buttonDownCount = 0;
            }
        }
        
        public void LeftStick(Vector2 stickPos)
        {
            _rawStickPos = stickPos;
        }

        public void ButtonStick(Vector2Int buttonStickPos)
        {
            _rawStickPos = NormalizedStickPos(buttonStickPos);

            Vector2 NormalizedStickPos(Vector2Int v)
            {
                const float factor = 1.0f / 32768.0f;
                return new Vector2(v.x * factor, v.y * factor);
            }
        }
        
        public override void Start()
        {
            //NOTE: 初期値が原点とかだと流石にキツいので、値を拾わせておく
            (_latestButtonPos, _latestButtonRot) = _stickProvider.GetRightHand(GamepadKey.A);
        }
        
        public override void Update()
        {
            //スティックについてはスティック値自体をLerpすることで平滑化
            _filterStickPos = Vector2.Lerp(_filterStickPos, _rawStickPos, LeftHandSpeedFactor * Time.deltaTime);
            var (leftPos, leftRot) = _stickProvider.GetLeftHand(_filterStickPos);
            _leftHand.Position = leftPos;
            _leftHand.Rotation = leftRot;
            
            //ボタンを押してる/押してないでy方向に移動が入ることに注意しつつ、素朴にLerp/Slerp
            //y軸方向の移動だけ更にシャープにするのもアリだが、書くのがめんどくさい…
            var offsetY = (_buttonDownCount > 0) ? -ButtonDownY : 0;
            var posWithOffset = _latestButtonPos + offsetY * _stickProvider.GetYAxis();
            _rightHand.Position = Vector3.Lerp(_rightHand.Position, posWithOffset, RightHandSpeedFactor * Time.deltaTime);
            _rightHand.Rotation = Quaternion.Slerp(_rightHand.Rotation, _latestButtonRot, RightHandSpeedFactor * Time.deltaTime);
        }
    }
}
