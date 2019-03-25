using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using XinputGamePad;

namespace Baku.VMagicMirror
{
    public class StatefulXinputGamePad : MonoBehaviour
    {
        public int DeviceNumber;

        public IObservable<XinputKeyData> ButtonUpDown => _buttonSubject;

        /// <summary>
        /// Position is (x, y), and both x and y are in short (MIN=-32768, MAX=+32767)
        /// </summary>
        public IObservable<Vector2Int> RightStickPosition => _rightStick;

        /// <summary>
        /// Position is (x, y), and both x and y are in short (MIN=-32768, MAX=+32767)
        /// </summary>
        public IObservable<Vector2Int> LeftStickPosition => _leftStick;

        private readonly HashSet<ObservableButton> _buttons = new HashSet<ObservableButton>();
        private readonly Subject<XinputKeyData> _buttonSubject = new Subject<XinputKeyData>();

        private readonly Subject<Vector2Int> _rightStick = new Subject<Vector2Int>();
        private readonly Subject<Vector2Int> _leftStick = new Subject<Vector2Int>();

        private Vector2Int _rightStickPosition = Vector2Int.zero;
        private Vector2Int _leftStickPosition = Vector2Int.zero;

        private void Start()
        {
            _buttons.Add(new ObservableButton(XinputKey.B, InputConst.XINPUT_GAMEPAD_B, _buttonSubject));
            _buttons.Add(new ObservableButton(XinputKey.A, InputConst.XINPUT_GAMEPAD_A, _buttonSubject));
            _buttons.Add(new ObservableButton(XinputKey.X, InputConst.XINPUT_GAMEPAD_X, _buttonSubject));
            _buttons.Add(new ObservableButton(XinputKey.Y, InputConst.XINPUT_GAMEPAD_Y, _buttonSubject));
            _buttons.Add(new ObservableButton(XinputKey.RIGHT, InputConst.XINPUT_GAMEPAD_DPAD_RIGHT, _buttonSubject));
            _buttons.Add(new ObservableButton(XinputKey.DOWN, InputConst.XINPUT_GAMEPAD_DPAD_DOWN, _buttonSubject));
            _buttons.Add(new ObservableButton(XinputKey.LEFT, InputConst.XINPUT_GAMEPAD_DPAD_LEFT, _buttonSubject));
            _buttons.Add(new ObservableButton(XinputKey.UP, InputConst.XINPUT_GAMEPAD_DPAD_UP, _buttonSubject));
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
        }

        public void Reset()
        {
            foreach (var button in _buttons)
            {
                button.Reset();
            }
            _rightStickPosition = Vector2Int.zero;
            _leftStickPosition = Vector2Int.zero;
        }

        public XinputTriger GetTriger() => new XinputTriger
        {
            Right = DllConst.GetRightTrigger(DeviceNumber),
            Left = DllConst.GetLeftTrigger(DeviceNumber)
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

        class ObservableButton
        {
            public ObservableButton(XinputKey key, int flag, Subject<XinputKeyData> subject)
            {
                _key = key;
                _flag = flag;
                _subject = subject;
            }

            private readonly XinputKey _key;
            private readonly int _flag;
            private readonly Subject<XinputKeyData> _subject;

            private bool _isPressed = false;
            public bool IsPressed
            {
                get => _isPressed;
                private set
                {
                    if (_isPressed != value)
                    {
                        _isPressed = value;
                        _subject.OnNext(new XinputKeyData(_key, IsPressed));
                    }
                }
            }

            public void Reset()
                => _isPressed = false;

            public void UpdatePressedState(int buttonStateFlags) 
                => IsPressed = ((buttonStateFlags & _flag) != 0);

        }

    }
}

