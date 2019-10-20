using System;
using System.Collections;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// ゲームパッドの入力状況に対して望ましい腕IKを指定するやつ。
    /// 従来版と違い、小さなゲームパッドを握っている状態を再現する狙いで実装している
    /// </summary>
    public class SmallGamepadHandIKGenerator : MonoBehaviour
    {
        [SerializeField] private SmallGamepadProvider gamePad = null;
        [SerializeField] private ImageBasedBodyMotion imageBasedBodyMotion = null;
        
        [Tooltip("ゲームパッド全体を動かす速度ファクタ")]
        [SerializeField] private float speedFactor = 3.0f;
        
        [Tooltip("ボタンを押す/押してないに依存して手を上下させる動きの速度ファクタ")]
        [SerializeField] private float buttonDownSpeedFactor = 8f;
        
        [Tooltip("体が動いた量をゲームパッドの移動量に反映するファクター")]
        [Range(0f, 1f)]
        [SerializeField] private float bodyMotionToGamepadPosApplyFactor = 0.5f;
        
        
        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKGenerator LeftHand => _leftHand;

        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKGenerator RightHand => _rightHand;

        private const float ButtonDownAnimationY = 0.01f;
        
        private Coroutine _buttonDownIkMotionCoroutine = null;

        //NOTE: raw系の値はゲームパッドの位置からただちに求まる、ローパスされていない値
        private Vector3 _rawLeftPos = Vector3.zero;
        private Quaternion _rawLeftRot = Quaternion.identity;
        
        private Vector3 _rawRightPos = Vector3.zero;
        private Quaternion _rawRightRot = Quaternion.identity;
        
        //NOTE: filter系の値はraw系の値にlerpがかかったやつ
        private Vector3 _filterLeftPos = Vector3.zero;
        private Quaternion _filterLeftRot = Quaternion.identity;
        
        private Vector3 _filterRightPos = Vector3.zero;
        private Quaternion _filterRightRot = Quaternion.identity;
        
        

        private float _offsetY = 0;
        private int _buttonDownCount = 0;
        private GamepadLeanModes _leanMode = GamepadLeanModes.GamepadLeanLeftStick;

        public bool ReverseGamepadStickLeanHorizontal { get; set; } = false;
        public bool ReverseGamepadStickLeanVertical { get; set; } = false;

        public void SetGamepadLeanMode(string leanModeName)
        {
            _leanMode =
                Enum.TryParse<GamepadLeanModes>(leanModeName, out var result) ?
                    result :
                    GamepadLeanModes.GamepadLeanNone;

            if (_leanMode == GamepadLeanModes.GamepadLeanNone)
            {
                ApplyStickPosition(Vector2Int.zero);
            }
        }
        
        public void ButtonDown(GamepadKey key)
        {
            if (GamepadProvider.IsSideKey(key))
            {
                return;
            }

            _buttonDownCount++;
        }

        public void ButtonUp(GamepadKey key)
        {
            if (GamepadProvider.IsSideKey(key))
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
            if (_leanMode == GamepadLeanModes.GamepadLeanLeftStick)
            {
                ApplyStickPosition(stickPos);
            }
        }

        public void RightStick(Vector2 stickPos)
        {
            if (_leanMode == GamepadLeanModes.GamepadLeanRightStick)
            {
                ApplyStickPosition(stickPos);
            }
        }

        public void ButtonStick(Vector2Int buttonStickPos)
        {
            if (_leanMode == GamepadLeanModes.GamepadLeanLeftButtons)
            {
                ApplyStickPosition(NormalizedStickPos(buttonStickPos));
            }
        }
        
        private void ApplyStickPosition(Vector2 stickPos)
        {
            var pos = new Vector2(
                stickPos.x * (ReverseGamepadStickLeanHorizontal ? -1f : 1f),
                stickPos.y * (ReverseGamepadStickLeanVertical ? -1f : 1f)
                );
            
            gamePad.SetHorizontalPosition(pos);
            (_rawLeftPos, _rawLeftRot) = gamePad.GetLeftHand();
            (_rawRightPos, _rawRightRot) = gamePad.GetRightHand();   
        }

        private static Vector2 NormalizedStickPos(Vector2Int v)
        {
            const float factor = 1.0f / 32768.0f;
            return new Vector2(v.x * factor, v.y * factor);
        }

        private void Start()
        {
            //とりあえず初期位置までゲームコントローラIKの場所を持ち上げておく:
            //やらないとIK位置が0,0,0のままになって良くない
            ApplyStickPosition(Vector2.zero);

            (_filterLeftPos, _filterLeftRot, _filterRightPos, _filterRightRot) =
                (_rawLeftPos, _rawLeftRot, _rawRightPos, _rawRightRot);
        }
        
        private void Update()
        {
            UpdateButtowDownYOffset();
            
            //とりあえず全部Lerp
            _filterLeftPos = Vector3.Lerp(_filterLeftPos, _rawLeftPos, speedFactor * Time.deltaTime);
            _filterLeftRot = Quaternion.Slerp(_filterLeftRot, _rawLeftRot, speedFactor * Time.deltaTime);
            _filterRightPos = Vector3.Lerp(_filterRightPos, _rawRightPos, speedFactor * Time.deltaTime);
            _filterRightRot = Quaternion.Slerp(_filterRightRot, _rawRightRot, speedFactor * Time.deltaTime);

            //ボタン押し状態、および体の動きを考慮して最終的なIKを適用
            _leftHand.Position = 
                _filterLeftPos +
                Vector3.up * _offsetY +
                imageBasedBodyMotion.BodyIkOffset * bodyMotionToGamepadPosApplyFactor;
            _leftHand.Rotation = _filterLeftRot;
            
            _rightHand.Position = 
                _filterRightPos +
                Vector3.up * _offsetY +
                imageBasedBodyMotion.BodyIkOffset * bodyMotionToGamepadPosApplyFactor;
            _rightHand.Rotation = _filterRightRot;
        }

        private void UpdateButtowDownYOffset()
        {
            float offsetGoal =
                (_buttonDownCount > 0) ? -ButtonDownAnimationY : 0;

            _offsetY = Mathf.Lerp(
                _offsetY,
                offsetGoal,
                buttonDownSpeedFactor * Time.deltaTime
                );
        }

        /// <summary> どの入力をBodyLeanの値に反映するか考慮するやつ </summary>
        private enum GamepadLeanModes
        {
            GamepadLeanNone,
            GamepadLeanLeftButtons,
            GamepadLeanLeftStick,
            GamepadLeanRightStick,
        }        
    }
}
