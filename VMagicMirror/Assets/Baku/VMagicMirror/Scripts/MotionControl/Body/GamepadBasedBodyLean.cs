using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// ゲームパッド入力に応じて体の傾き量を計算するやつ
    /// </summary>
    public class GamepadBasedBodyLean : MonoBehaviour
    {
        private const float BodyLeanSpeedFactor = 3.0f;
        
        [SerializeField] private Vector2 bodyLeanMaxAngle = new Vector2(2.0f, 2.0f);
        
        public Quaternion BodyLeanSuggest { get; private set; } = Quaternion.identity;
        public bool ReverseGamepadStickLeanHorizontal { get; set; } = false;
        public bool ReverseGamepadStickLeanVertical { get; set; } = false;
        
        private Quaternion _target = Quaternion.identity;
        private GamepadLeanModes _leanMode = GamepadLeanModes.GamepadLeanLeftStick;

        private void Update()
        {
            BodyLeanSuggest = Quaternion.Slerp(
                BodyLeanSuggest,
                _target,
                BodyLeanSpeedFactor * Time.deltaTime
                );
        }
        
        public void SetGamepadLeanMode(string leanModeName)
        {
            _leanMode =
                Enum.TryParse<GamepadLeanModes>(leanModeName, out var result) ?
                    result :
                    GamepadLeanModes.GamepadLeanNone;

            if (_leanMode == GamepadLeanModes.GamepadLeanNone)
            {
                ApplyLeanMotion(Vector2Int.zero);
            }
        }
        
        public void LeftStick(Vector2Int stickPos)
        {
            if (_leanMode == GamepadLeanModes.GamepadLeanLeftStick)
            {
                ApplyLeanMotion(stickPos);
            }
        }

        public void RightStick(Vector2Int stickPos)
        {
            if (_leanMode == GamepadLeanModes.GamepadLeanRightStick)
            {
                ApplyLeanMotion(stickPos);
            }
        }

        /// <summary>
        /// NOTE: ButtonStickは十字キーボタンをベースに求めた便宜的なスティック位置のことです
        /// </summary>
        /// <param name="buttonStickPos"></param>
        public void ButtonStick(Vector2Int buttonStickPos)
        {
            if (_leanMode == GamepadLeanModes.GamepadLeanLeftButtons)
            {
                ApplyLeanMotion(buttonStickPos);
            }
        }
        
        private void ApplyLeanMotion(Vector2Int stickPos)
        {
            var normalized = NormalizedStickPos(stickPos);

            var pos = new Vector2(
                normalized.x * (ReverseGamepadStickLeanHorizontal ? -1f : 1f),
                normalized.y * (ReverseGamepadStickLeanVertical ? -1f : 1f)
                );
            
            _target = Quaternion.Euler(
                pos.y * bodyLeanMaxAngle.y,
                0,
                -pos.x * bodyLeanMaxAngle.x
                );
        }

        private static Vector2 NormalizedStickPos(Vector2Int v)
        {
            const float factor = 1.0f / 32768.0f;
            return new Vector2(v.x * factor, v.y * factor);
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
