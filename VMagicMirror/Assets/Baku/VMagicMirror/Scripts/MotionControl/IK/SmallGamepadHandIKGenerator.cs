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
        [SerializeField] private float speedFactor = 3.0f;
        
        private readonly IKDataRecord _leftHand = new IKDataRecord();
        public IIKGenerator LeftHand => _leftHand;

        private readonly IKDataRecord _rightHand = new IKDataRecord();
        public IIKGenerator RightHand => _rightHand;

        private const float ButtonDownAnimationY = 0.01f;
        private const float ButtonDownAnimationDuration = 0.2f;
        
        private Coroutine _buttonDownIkMotionCoroutine = null;

        private Vector3 _rawLeftPos = Vector3.zero;
        private Quaternion _rawLeftRot = Quaternion.identity;
        
        private Vector3 _rawRightPos = Vector3.zero;
        private Quaternion _rawRightRot = Quaternion.identity;

        private float _offsetY = 0;
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
            
            //親指で押すボタンの場合、手をクイッてする動きとして反映
            if (_buttonDownIkMotionCoroutine != null)
            {
                StopCoroutine(_buttonDownIkMotionCoroutine);
            }
            _buttonDownIkMotionCoroutine = StartCoroutine(ButtonDownRoutine());
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
        
        private void Update()
        {
            _leftHand.Position =
                Vector3.Lerp(_leftHand.Position, _rawLeftPos, speedFactor * Time.deltaTime) +
                Vector3.up * _offsetY;

            _leftHand.Rotation = Quaternion.Slerp(
                _leftHand.Rotation, _rawLeftRot, speedFactor * Time.deltaTime
            );
            
            _rightHand.Position =
                Vector3.Lerp(_rightHand.Position, _rawRightPos, speedFactor * Time.deltaTime) +
                Vector3.up * _offsetY;

            _rightHand.Rotation = Quaternion.Slerp(
                _rightHand.Rotation, _rawRightRot, speedFactor * Time.deltaTime
            );
            
        }
        
        private IEnumerator ButtonDownRoutine()
        {
            float startTime = Time.time;
            while (Time.time - startTime < ButtonDownAnimationDuration)
            {
                float rate = (Time.time - startTime) / ButtonDownAnimationDuration;
                _offsetY = -ButtonDownAnimationY * (1.0f - Mathf.Abs(1.0f - 2.0f * rate));
                yield return null;
            }
            _offsetY = 0;
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
