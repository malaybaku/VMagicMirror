using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using XinputGamePad;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// ゲームパッドの状態を通知してくれるやつ
    /// </summary>
    public class StatefulXinputGamePad : MonoBehaviour
    {
        [SerializeField] private int triggerDownThreshold = 30;
        [SerializeField] private int deviceNumber;

        private const int StickPositionDiffThreshold = 1000;
        
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

        //DirectInputによって入力キャプチャをこっそり代行してくれるやつ
        private readonly DirectInputGamePad _directInputAlternative = new DirectInputGamePad();
        
        //このクラス自身がforeachで使うときはこっち
        private HashSet<ObservableButton> _buttons = new HashSet<ObservableButton>();
        //DirectInput入力で代わりに上書きするときはここからアクセス
        private readonly List<ObservableButton> _buttonsList = new List<ObservableButton>(16);
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

        //Updateで実処理を呼んでもいいかどうか
        private bool _updateEnabled = true;
        //XInputよりもDirectInputで取得できるコントローラを使うべきかどうか(PS4コンではtrue)
        private bool _preferDirectInput = false;

        public void SetEnableGamepad(bool enableGamepad)
        {
            LogOutput.Instance.Write("SetEnableGamepad");
            _updateEnabled = enableGamepad;
            //DirectInputの場合、明示的にデバイスを捕まえる必要があるので捕まえておく
            if (_preferDirectInput)
            {
                if (enableGamepad)
                {
                    _directInputAlternative.ConnectToDevice(NativeMethods.GetUnityWindowHandle());
                }
                else
                {
                    _directInputAlternative.Stop();
                }
            }
        }
        
        public void SetPreferDirectInputGamepad(bool preferDirectInput)
        {
            LogOutput.Instance.Write("SetPreferDirectInputGamepad");
            _preferDirectInput = preferDirectInput;
            //読み取り中のままXInputとDirectInputを切り替え: この場合、DirectInput側だけ読み取り開始や停止が起きる
            if (_updateEnabled)
            {
                if (preferDirectInput)
                {
                    _directInputAlternative.ConnectToDevice(NativeMethods.GetUnityWindowHandle());
                }
                else
                {
                    _directInputAlternative.Stop();
                }
            }
        }        
        
        private void Start()
        {
            _buttonsList.Add(new ObservableButton(GamepadKey.Start, InputConst.XINPUT_GAMEPAD_START, _buttonSubject));
            
            _buttonsList.Add(new ObservableButton(GamepadKey.B, InputConst.XINPUT_GAMEPAD_B, _buttonSubject));
            _buttonsList.Add(new ObservableButton(GamepadKey.A, InputConst.XINPUT_GAMEPAD_A, _buttonSubject));
            _buttonsList.Add(new ObservableButton(GamepadKey.X, InputConst.XINPUT_GAMEPAD_X, _buttonSubject));
            _buttonsList.Add(new ObservableButton(GamepadKey.Y, InputConst.XINPUT_GAMEPAD_Y, _buttonSubject));

            _buttonsList.Add(new ObservableButton(GamepadKey.RShoulder, InputConst.XINPUT_GAMEPAD_RIGHT_SHOULDER, _buttonSubject));
            _buttonsList.Add(new ObservableButton(GamepadKey.LShoulder, InputConst.XINPUT_GAMEPAD_LEFT_SHOULDER, _buttonSubject));
            
            _arrowRight = new ObservableButton(GamepadKey.RIGHT, InputConst.XINPUT_GAMEPAD_DPAD_RIGHT, _buttonSubject);
            _arrowDown = new ObservableButton(GamepadKey.DOWN, InputConst.XINPUT_GAMEPAD_DPAD_DOWN, _buttonSubject);
            _arrowLeft = new ObservableButton(GamepadKey.LEFT, InputConst.XINPUT_GAMEPAD_DPAD_LEFT, _buttonSubject);
            _arrowUp = new ObservableButton(GamepadKey.UP, InputConst.XINPUT_GAMEPAD_DPAD_UP, _buttonSubject);

            _buttonsList.Add(_arrowRight);
            _buttonsList.Add(_arrowDown);
            _buttonsList.Add(_arrowLeft);
            _buttonsList.Add(_arrowUp);
            
            _buttons = new HashSet<ObservableButton>(_buttonsList);
            _hasValidArrowButtons = true;
        }
        
        private void Update()
        {
            if (!_updateEnabled)
            {
                return;
            }

            if (_preferDirectInput)
            {
                //DirectInputの読み取り機能で更新
                _directInputAlternative.Update();
                UpdateByState(_directInputAlternative.CurrentState);
            }
            else
            {
                //普通にXInputの読み取り
                DllConst.Capture();
                int buttonFlags = DllConst.GetButtons(deviceNumber);
                foreach(var button in _buttons)
                {
                    button.UpdatePressedState(buttonFlags);
                }
                UpdateRightStick();
                UpdateLeftStick();
                UpdateTriggerAsButtons();
            }
        }

        private void OnDestroy() => _directInputAlternative.Stop();

        private void UpdateByState(GamepadState state)
        {
            //NOTE: ボタンの順序はStart()で初期化してる順番と揃えてます
            _buttonsList[0].IsPressed = state.Start;
            
            _buttonsList[1].IsPressed = state.B;
            _buttonsList[2].IsPressed = state.A;
            _buttonsList[3].IsPressed = state.X;
            _buttonsList[4].IsPressed = state.Y;
            
            _buttonsList[5].IsPressed = state.R1;
            _buttonsList[6].IsPressed = state.L1;
            
            _buttonsList[7].IsPressed = state.Right;
            _buttonsList[8].IsPressed = state.Down;
            _buttonsList[9].IsPressed = state.Left;
            _buttonsList[10].IsPressed = state.Up;
            
            var right = new Vector2Int(state.RightX, state.RightY);
            if (Mathf.Abs(right.x - _rightStickPosition.x) + 
                Mathf.Abs(right.y - _rightStickPosition.y) > StickPositionDiffThreshold)
            {
                _rightStickPosition = right;
                _rightStick.OnNext(right);
            }
            
            var left = new Vector2Int(state.LeftX, state.LeftY);
            if (Mathf.Abs(left.x - _leftStickPosition.x) + 
                Mathf.Abs(left.y - _leftStickPosition.y) > StickPositionDiffThreshold)
            {
                _leftStickPosition = left;
                _leftStick.OnNext(left);
            }

            //トリガー情報はDirectInputの場合ボタンベースで取得する。
            //DUAL SHOCK 4のトリガーは連続値+ボタン情報で渡ってくるのでボタン情報だけ拾って使っている、という感じ。
            if (_isRightTriggerDown != state.R2)
            {
                _isRightTriggerDown = state.R2;
                _buttonSubject.OnNext(new GamepadKeyData(GamepadKey.RTrigger, _isRightTriggerDown));
            }
            
            if (_isLeftTriggerDown != state.L2)
            {
                _isLeftTriggerDown = state.L2;
                _buttonSubject.OnNext(new GamepadKeyData(GamepadKey.LTrigger, _isLeftTriggerDown));
            }
        }

        private void UpdateRightStick()
        {
            var position = new Vector2Int(
                DllConst.GetThumbRX(deviceNumber),
                DllConst.GetThumbRY(deviceNumber)
                );
            
            if (Mathf.Abs(_rightStickPosition.x - position.x) +
                Mathf.Abs(_rightStickPosition.y - position.y) > StickPositionDiffThreshold)
            {
                _rightStickPosition = position;
                _rightStick.OnNext(position);
            }
        }

        private void UpdateLeftStick()
        {
            var position = new Vector2Int(
                DllConst.GetThumbLX(deviceNumber),
                DllConst.GetThumbLY(deviceNumber)
                );

            if (Mathf.Abs(_leftStickPosition.x - position.x) +
                Mathf.Abs(_leftStickPosition.y - position.y) > StickPositionDiffThreshold)
            {
                _leftStickPosition = position;
                _leftStick.OnNext(position);
            }
        }

        private void UpdateTriggerAsButtons()
        {
            int right = DllConst.GetRightTrigger(deviceNumber);
            bool isRightDown = (right > triggerDownThreshold);
            if (_isRightTriggerDown != isRightDown)
            {
                _isRightTriggerDown = isRightDown;
                _buttonSubject.OnNext(new GamepadKeyData(GamepadKey.RTrigger, isRightDown));
            }
            
            int left = DllConst.GetLeftTrigger(deviceNumber);
            bool isLeftDown = (left > triggerDownThreshold);
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
                set
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
        Start,
        Select,
    }
    
    /// <summary> ゲームパッドの状態を渡す用のクラス </summary>
    public class GamepadState
    {
        public bool IsValid { get; set; }

        public bool A { get; set; }
        public bool B { get; set; }
        public bool X { get; set; }
        public bool Y { get; set; }
        public bool Start { get; set; }
        public bool Select { get; set; }

        //NOTE: L2/R2についてはトリガーの連続値を捨ててオンオフにしてます
        public bool R1 { get; set; }
        public bool R2 { get; set; }
        public bool R3 { get; set; }
        public bool L1 { get; set; }
        public bool L2 { get; set; }
        public bool L3 { get; set; }


        public bool Up { get; set; }
        public bool Down { get; set; }
        public bool Left { get; set; }
        public bool Right { get; set; }

        //この4つの値は-32768 ~ 32767
        public int LeftX { get; set; }
        public int LeftY { get; set; }
        public int RightX { get; set; }
        public int RightY { get; set; }

        public void Reset()
        {
            IsValid = false;
            A = false;
            B = false;
            X = false;
            Y = false;

            Start = false;
            Select = false;

            R1 = false;
            R2 = false;
            R3 = false;
            L1 = false;
            L2 = false;
            L3 = false;

            Up = false;
            Right = false;
            Down = false;
            Left = false;

            LeftX = 0;
            LeftY = 0;
            RightX = 0;
            RightY = 0;
        }

    }    
}

