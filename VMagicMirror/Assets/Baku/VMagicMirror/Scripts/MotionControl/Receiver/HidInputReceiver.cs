using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    //note: こう書いてて思ったが、顔認識もReceiver的なフローに載せた方がいい？
    
    /// <summary>
    /// プロセス内外の両方から飛んできたHID(現状ではキーボード、マウス、コントローラ)の入力を集約して投げるクラス
    /// </summary>
    public class HidInputReceiver : MonoBehaviour
    {
        [Inject] private RawInputChecker _rawInputChecker = null;
        
        [SerializeField] private StatefulXinputGamePad gamePad = null;
        
        [SerializeField] private HandIKIntegrator handIkIntegrator = null;

        [SerializeField] private HeadIkIntegrator headIkIntegrator = null;
        
        [SerializeField] private GamepadBasedBodyLean gamepadBasedBodyLean = null;
        
        private bool _mousePositionInitialized = false;
        private int _mouseX = 0;
        private int _mouseY = 0;
        
        private void Start()
        {
            _rawInputChecker.PressedKeys.Subscribe(ReceiveKeyPressed);
            _rawInputChecker.MouseButton.Subscribe(ReceiveMouseButton);
            
            gamePad.ButtonUpDown.Subscribe(data =>
            {
                if (data.IsPressed)
                {
                    handIkIntegrator.GamepadButtonDown(data.Key);
                }
                else
                {
                    handIkIntegrator.GamepadButtonUp(data.Key);
                }

                if (data.IsArrowKey)
                {
                    gamepadBasedBodyLean.ButtonStick(gamePad.ArrowButtonsStickPosition);
                    handIkIntegrator.ButtonStick(gamePad.ArrowButtonsStickPosition);
                }
            });
            
            gamePad.LeftStickPosition.Subscribe(pos =>
            {
                var stickPos = NormalizedStickPos(pos);
                handIkIntegrator.MoveLeftGamepadStick(stickPos);
                gamepadBasedBodyLean.LeftStick(pos);
            });

            gamePad.RightStickPosition.Subscribe(pos =>
            {
                var stickPos = NormalizedStickPos(pos);
                handIkIntegrator.MoveRightGamepadStick(stickPos);
                gamepadBasedBodyLean.RightStick(pos);
            });
        }

        private void Update()
        {
            //NOTE: Unityが非アクティブのときの値取得については Run in Backgroundを前提として
            // - マウス位置: OK, Input.mousePositionで取れる。
            // - マウスクリック: NG, グローバルフック必須
            // - キーボード: NG, グローバルフック必須
            var pos = Input.mousePosition;
            if (!_mousePositionInitialized)
            {
                _mouseX = (int)pos.x;
                _mouseY = (int)pos.y;
                _mousePositionInitialized = true;
            }

            if (_mouseX != (int)pos.x || 
                _mouseY != (int)pos.y
                )
            {
                _mouseX = (int)pos.x;
                _mouseY = (int)pos.y;
                handIkIntegrator.MoveMouse(pos);
                headIkIntegrator.MoveMouse(_mouseX, _mouseY);
            }
        }
        
        private static Vector2 NormalizedStickPos(Vector2Int v)
        {
            const float factor = 1.0f / 32768.0f;
            return new Vector2(v.x * factor, v.y * factor);
        }
        
        private void ReceiveKeyPressed(string keyCodeName)
        {
            handIkIntegrator.PressKey(keyCodeName);
        }

        private void ReceiveMouseButton(string info)
        {
            if (info.Contains("Down"))
            {
                handIkIntegrator.ClickMouse(info);
            }
        }
    }
}
