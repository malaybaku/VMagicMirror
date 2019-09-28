using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using XinputGamePad;

namespace Baku.VMagicMirror
{
    public class StatefulXinputGamePad : MonoBehaviour
    {
        [SerializeField] private int TriggerDownThreshold = 30;
        
        public int DeviceNumber;

        public IObservable<GamepadKeyData> ButtonUpDown => _buttonSubject;

        /// <summary>
        /// Position is (x, y), and both x and y are in short (MIN=-32768, MAX=+32767)
        /// </summary>
        public IObservable<Vector2Int> RightStickPosition => _rightStick;

        /// <summary>
        /// Position is (x, y), and both x and y are in short (MIN=-32768, MAX=+32767)
        /// </summary>
        public IObservable<Vector2Int> LeftStickPosition => _leftStick;

        /// <summary>
        /// 矢印キーの押下状態からスティック位置に相当する情報を作成し、取得する。
        /// </summary>
        public Vector2Int ArrowButtonsStickPosition
        {          
            get
            {
                if (!_hasValidArrowButtons)
                {
                    return Vector2Int.zero;
                }

                return new Vector2Int(
                    (_arrowRight.IsPressed ? 32767 : 0) - (_arrowLeft.IsPressed ? 32768 : 0),
                    (_arrowUp.IsPressed ? 32767 : 0) - (_arrowDown.IsPressed ? 32768 : 0)
                    );
            }
        }

        private readonly HashSet<ObservableButton> _buttons = new HashSet<ObservableButton>();
        private readonly Subject<GamepadKeyData> _buttonSubject = new Subject<GamepadKeyData>();

        private readonly Subject<Vector2Int> _rightStick = new Subject<Vector2Int>();
        private readonly Subject<Vector2Int> _leftStick = new Subject<Vector2Int>();

        private Vector2Int _rightStickPosition = Vector2Int.zero;
        private Vector2Int _leftStickPosition = Vector2Int.zero;

        private bool _hasValidArrowButtons = false;
        private ObservableButton _arrowRight;
        private ObservableButton _arrowDown;
        private ObservableButton _arrowLeft;
        private ObservableButton _arrowUp;

        private bool _isLeftTriggerDown = false;
        private bool _isRightTriggerDown = false;

        private void Start()
        {
            _buttons.Add(new ObservableButton(GamepadKey.B, InputConst.XINPUT_GAMEPAD_B, _buttonSubject));
            _buttons.Add(new ObservableButton(GamepadKey.A, InputConst.XINPUT_GAMEPAD_A, _buttonSubject));
            _buttons.Add(new ObservableButton(GamepadKey.X, InputConst.XINPUT_GAMEPAD_X, _buttonSubject));
            _buttons.Add(new ObservableButton(GamepadKey.Y, InputConst.XINPUT_GAMEPAD_Y, _buttonSubject));

            _buttons.Add(new ObservableButton(GamepadKey.RShoulder, InputConst.XINPUT_GAMEPAD_RIGHT_SHOULDER, _buttonSubject));
            _buttons.Add(new ObservableButton(GamepadKey.LShoulder, InputConst.XINPUT_GAMEPAD_LEFT_SHOULDER, _buttonSubject));
            
            _arrowRight = new ObservableButton(GamepadKey.RIGHT, InputConst.XINPUT_GAMEPAD_DPAD_RIGHT, _buttonSubject);
            _arrowDown = new ObservableButton(GamepadKey.DOWN, InputConst.XINPUT_GAMEPAD_DPAD_DOWN, _buttonSubject);
            _arrowLeft = new ObservableButton(GamepadKey.LEFT, InputConst.XINPUT_GAMEPAD_DPAD_LEFT, _buttonSubject);
            _arrowUp = new ObservableButton(GamepadKey.UP, InputConst.XINPUT_GAMEPAD_DPAD_UP, _buttonSubject);

            _buttons.Add(_arrowRight);
            _buttons.Add(_arrowDown);
            _buttons.Add(_arrowLeft);
            _buttons.Add(_arrowUp);
            _hasValidArrowButtons = true;
        }

        private void Update()
        {
            DllConst.Capture();
            int buttonFlags = DllConst.GetButtons(DeviceNumber);
            foreach(var button in _buttons)
            {
                button.UpdatePressedState(buttonFlags);
            }

            UpdateRightStick();
            UpdateLeftStick();
            UpdateTriggerAsButtons();
        }

        public void ResetControllerState()
        {
            foreach (var button in _buttons)
            {
                button.Reset();
            }
            _rightStickPosition = Vector2Int.zero;
            _leftStickPosition = Vector2Int.zero;
        }

        public XinputTriger GetTrigger() => new XinputTriger
        {
        };

        private void UpdateRightStick()
        {
            var position = new Vector2Int(
                DllConst.GetThumbRX(DeviceNumber),
                DllConst.GetThumbRY(DeviceNumber)
                );
            
            if (_rightStickPosition != position)
            {
                _rightStickPosition = position;
                _rightStick.OnNext(position);
            }
        }

        private void UpdateLeftStick()
        {
            var position = new Vector2Int(
                DllConst.GetThumbLX(DeviceNumber),
                DllConst.GetThumbLY(DeviceNumber)
                );

            if (_leftStickPosition != position)
            {
                _leftStickPosition = position;
                _leftStick.OnNext(position);
            }
        }

        private void UpdateTriggerAsButtons()
        {
            int right = DllConst.GetRightTrigger(DeviceNumber);
            bool isRightDown = (right > TriggerDownThreshold);
            if (_isRightTriggerDown != isRightDown)
            {
                _isRightTriggerDown = isRightDown;
                _buttonSubject.OnNext(new GamepadKeyData(GamepadKey.RTrigger, isRightDown));
            }
            
            int left = DllConst.GetLeftTrigger(DeviceNumber);
            bool isLeftDown = (left > TriggerDownThreshold);
            if (_isLeftTriggerDown != isLeftDown)
            {
                _isLeftTriggerDown = isLeftDown;
                _buttonSubject.OnNext(new GamepadKeyData(GamepadKey.LTrigger, isLeftDown));
            }
        }
        
        class ObservableButton
        {
            public ObservableButton(GamepadKey key, int flag, Subject<GamepadKeyData> subject)
            {
                _key = key;
                _flag = flag;
                _subject = subject;
            }

            private readonly GamepadKey _key;
            private readonly int _flag;
            private readonly Subject<GamepadKeyData> _subject;

            private bool _isPressed = false;
            public bool IsPressed
            {
                get => _isPressed;
                private set
                {
                    if (_isPressed != value)
                    {
                        _isPressed = value;
                        _subject.OnNext(new GamepadKeyData(_key, IsPressed));
                    }
                }
            }

            public void Reset()
                => _isPressed = false;

            public void UpdatePressedState(int buttonStateFlags) 
                => IsPressed = ((buttonStateFlags & _flag) != 0);

        }

    }

    /// <summary>
    /// NOTE: Baku.VMagicMirror内部でのボタンの呼称。DirectInputに後で対応したときもコレを使う
    /// </summary>
    public enum GamepadKey
    {
        LEFT,
        RIGHT,
        UP,
        DOWN,
        A,
        B,
        X,
        Y,
        RShoulder,
        LShoulder,
        //NOTE: トリガーキーも便宜的にon/offのボタン扱いする
        RTrigger,
        LTrigger,
    }
}

