using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    //note: こう書いてて思ったが、顔認識もReceiver的なフローに載せた方がいい？
    
    /// <summary>
    /// プロセス内外の両方から飛んできたHID(キーボード、マウス、コントローラ、MIDI)の入力を集約して投げるクラス
    /// </summary>
    public class HidInputReceiver : MonoBehaviour
    {
        [SerializeField] private HandIKIntegrator handIkIntegrator = null;
        [SerializeField] private HeadIkIntegrator headIkIntegrator = null;
        [SerializeField] private GamepadBasedBodyLean gamepadBasedBodyLean = null;
        
        [Inject] private RawInputChecker _rawInput = null;
        [Inject] private StatefulXinputGamePad _gamePad = null;
        [Inject] private MidiInputObserver _midiInput = null;
        
        private bool _mousePositionInitialized = false;
        private int _mouseX = 0;
        private int _mouseY = 0;
        
        private void Start()
        {
            _rawInput.PressedKeys.Subscribe(ReceiveKeyPressed);
            _rawInput.MouseButton.Subscribe(ReceiveMouseButton);
            
            _gamePad.ButtonUpDown.Subscribe(data =>
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
                    gamepadBasedBodyLean.ButtonStick(_gamePad.ArrowButtonsStickPosition);
                    handIkIntegrator.ButtonStick(_gamePad.ArrowButtonsStickPosition);
                }
            });
            
            _gamePad.LeftStickPosition.Subscribe(pos =>
            {
                var stickPos = NormalizedStickPos(pos);
                handIkIntegrator.MoveLeftGamepadStick(stickPos);
                gamepadBasedBodyLean.LeftStick(pos);
            });

            _gamePad.RightStickPosition.Subscribe(pos =>
            {
                var stickPos = NormalizedStickPos(pos);
                handIkIntegrator.MoveRightGamepadStick(stickPos);
                gamepadBasedBodyLean.RightStick(pos);
            });

            _midiInput.NoteOn.Subscribe(note =>
            {
                handIkIntegrator.NoteOn(note);
            });

            _midiInput.KnobValue.Subscribe(OnKnob);
            void OnKnob((int knob, float value) data) 
                => handIkIntegrator.KnobValueChange(data.knob, data.value);
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
