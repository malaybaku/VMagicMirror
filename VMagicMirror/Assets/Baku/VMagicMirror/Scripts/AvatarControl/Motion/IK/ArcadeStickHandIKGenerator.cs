using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// ゲームパッドの入力状況に対してアーケードスティック型の腕IKを生成するやつ。
    /// </summary>
    public class ArcadeStickHandIKGenerator : HandIkGeneratorBase
    {
        [Serializable]
        public class GamepadHandIkGeneratorSetting
        {
            public ImageBasedBodyMotion imageBasedBodyMotion;            
        }

        // ゲームパッド全体を動かす速度ファクタ
        private const float SpeedFactor = 3.0f;
        
        // ボタンを押す/押してないに依存して手を上下させる動きの速度ファクタ
        private const float ButtonDownSpeedFactor = 8f;
        
        //体が動いた量をゲームパッドの移動量に反映するファクター
        private const float BodyMotionToGamepadPosApplyFactor = 0.5f;

        private const float OffsetResetLerpFactor = 6f;
        
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
                float height = h.position.y - f.position.y;
                _bodySizeScale = Mathf.Clamp(height / ReferenceHeight, 0.1f, 5f);
            };
        }
        
        private readonly ArcadeStickProvider _stickProvider;
        private readonly GamepadHandIkGeneratorSetting _setting;
        
        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKGenerator LeftHand => _leftHand;

        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKGenerator RightHand => _rightHand;

        private float _bodySizeScale;

        private const float ButtonDownAnimationY = 0.01f;
        private const float ReferenceHeight = 1.3f;
        
        private Vector2 _rawStickPos = Vector2.zero;
        private Vector2 _filterStickPos = Vector2.zero;
        
        private float _offsetY = 0;
        private int _buttonDownCount = 0;

        private Vector3 _latestButtonPos;
        private Quaternion _latestButtonRot;
        
        public void ButtonDown(GamepadKey key)
        {
            if (ArcadeStickProvider.IsArcadeStickKey(key))
            {
                return;
            }

            _buttonDownCount++;

            (_latestButtonPos, _latestButtonRot) = _stickProvider.GetRightHand(key);
        }

        public void ButtonUp(GamepadKey key)
        {
            if (ArcadeStickProvider.IsArcadeStickKey(key))
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
            UpdateButtonDownYOffset();            
            
            //スティックについてはスティック値自体をLerpすることで平滑化
            _filterStickPos = Vector2.Lerp(_filterStickPos, _rawStickPos, SpeedFactor * Time.deltaTime);
            var (leftPos, leftRot) = _stickProvider.GetLeftHand(_filterStickPos);
            _leftHand.Position = leftPos;
            _leftHand.Rotation = leftRot;
            
            //ボタン側は押してる/押してないでy方向にちょっと動くことに注意しつつ、素朴にLerp/Slerp

            var posWithOffset = _latestButtonPos + _offsetY * _stickProvider.GetYAxis();
            
            _rightHand.Position = Vector3.Lerp(_rightHand.Position, _latestButtonPos, SpeedFactor * Time.deltaTime);
            _rightHand.Rotation = Quaternion.Slerp(_rightHand.Rotation, _latestButtonRot, SpeedFactor * Time.deltaTime);
        }

        private void UpdateButtonDownYOffset()
        {
            float offsetGoal =
                (_buttonDownCount > 0) ? -ButtonDownAnimationY : 0;

            _offsetY = Mathf.Lerp(
                _offsetY,
                offsetGoal,
                ButtonDownSpeedFactor * Time.deltaTime
                );
        }
        
    }
}
