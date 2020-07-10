using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using mattatz.TransformControl;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// キーボードやマウスパッドの位置をユーザーが自由に編集できるかどうかを設定するレシーバークラス
    /// UIが必要になるので、そのUIの操作もついでにここでやります
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class DeviceTransformControlReceiver : MonoBehaviour
    {
        //NOTE: このクラスでanimatorとかを直読みしたくないので、リセット処理を外注します
        [SerializeField] private Slider gamepadModelScaleSlider = null;
        
        private IMessageSender _sender;
        
        private SettingAutoAdjuster _settingAutoAdjuster = null;
        private TransformControl _keyboardControl = null;
        private TransformControl _touchPadControl = null;
        private TransformControl _midiControl = null;
        private TransformControl _gamepadControl= null;
        private Transform _gamepadModelScaleTarget = null;
        
        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            IMessageSender sender,
            SettingAutoAdjuster settingAutoAdjuster,
            KeyboardProvider keyboard,
            TouchPadProvider touchPad,
            MidiControllerProvider midiController,
            SmallGamepadProvider gamepad
            )
        {
            _sender = sender;
            _settingAutoAdjuster = settingAutoAdjuster;

            _keyboardControl = keyboard.TransformControl;
            _touchPadControl = touchPad.TransformControl;
            _midiControl = midiController.TransformControl;
            _gamepadControl = gamepad.TransformControl;
            _gamepadModelScaleTarget = gamepad.ModelScaleTarget;
            
            receiver.AssignCommandHandler(
                MessageCommandNames.EnableDeviceFreeLayout,
                command => EnableDeviceFreeLayout(command.ToBoolean())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.SetDeviceLayout,
                command => SetDeviceLayout(command.Content)
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.ResetDeviceLayout,
                command => ResetDeviceLayout()
                );
        }

        public TransformControl KeyboardControl => _keyboardControl;
        public TransformControl TouchPadControl => _touchPadControl;
        public TransformControl GamepadControl => _gamepadControl;
        public TransformControl MidiControl => _midiControl;

        public bool CanShowKeyboardControl { get; set; } = true;
        public bool CanShowTouchpadControl { get; set; } = true;
        public bool CanShowGamepadControl { get; set; } = true;
        public bool CanShowMidiControl { get; set; } = true;

        private TransformControl[] _transformControls => new[]
        {
            _keyboardControl,
            _touchPadControl,
            _midiControl,
            _gamepadControl,
        };

        private bool _isDeviceFreeLayoutEnabled = false;

        public bool IsDeviceFreeLayoutEnabled
        {
            get => _isDeviceFreeLayoutEnabled;
            private set
            {
                if (_isDeviceFreeLayoutEnabled == value)
                {
                    return;
                }

                _isDeviceFreeLayoutEnabled = value;
                if (!value)
                {
                    SendDeviceLayoutData();
                }
            }
        }
        
        private bool _preferWorldCoordinate = false;
        private TransformControl.TransformMode _mode = TransformControl.TransformMode.Translate;
        private Canvas _canvas = null;
        
        private void Start()
        {
            _canvas = GetComponent<Canvas>();
        }

        private void Update()
        {
            if (!IsDeviceFreeLayoutEnabled)
            {
                return;
            }

            _keyboardControl.mode = CanShowKeyboardControl ? _mode : TransformControl.TransformMode.None;
            _touchPadControl.mode = CanShowTouchpadControl ? _mode : TransformControl.TransformMode.None;
            _gamepadControl.mode = CanShowGamepadControl ? _mode : TransformControl.TransformMode.None;
            _midiControl.mode = CanShowMidiControl ? _mode : TransformControl.TransformMode.None;
            
            for (int i = 0; i < _transformControls.Length; i++)
            {
                _transformControls[i].Control();
            }
        }

        private void EnableDeviceFreeLayout(bool enable)
        {
            Debug.Log("Enable Device Free Layout: " + enable);
            if (IsDeviceFreeLayoutEnabled == enable)
            {
                return;
            }
            
            IsDeviceFreeLayoutEnabled = enable;
            _canvas.enabled = enable;
            for (int i = 0; i < _transformControls.Length; i++)
            {
                _transformControls[i].enabled = enable;
                _transformControls[i].mode = enable ? _mode : TransformControl.TransformMode.None;
            }
        }

        private void SendDeviceLayoutData()
        {
            var data = new DeviceLayoutsData()
            {
                keyboard = ToItem(_keyboardControl.transform),
                touchPad = ToItem(_touchPadControl.transform),
                midi = ToItem(_midiControl.transform),
                gamepad =  ToItem(_gamepadControl.transform),
                gamepadModelScale = _gamepadModelScaleTarget.localScale.x,
            };
            _sender?.SendCommand(MessageFactory.Instance.UpdateDeviceLayout(data));

            DeviceLayoutItem ToItem(Transform t)
            {
                //NOTE: localScaleだけローカルだが、そもそも3つのTransformControlはぜんぶルート階層にある前提になってます
                return new DeviceLayoutItem()
                {
                    pos = t.position,
                    rot = t.rotation.eulerAngles,
                    scale = t.localScale,
                };
            }
        }
                
        private void SetDeviceLayout(string content)
        {
            try
            {
                var data = JsonUtility.FromJson<DeviceLayoutsData>(content);
                ApplyItem(data.keyboard, _keyboardControl.transform);
                ApplyItem(data.touchPad, _touchPadControl.transform);
                ApplyItem(data.midi, _midiControl.transform);
                ApplyItem(data.gamepad, _gamepadControl.transform);
                gamepadModelScaleSlider.value = Mathf.Clamp(
                    data.gamepadModelScale,
                    gamepadModelScaleSlider.minValue,
                    gamepadModelScaleSlider.maxValue);
                //NOTE: ここは念押しでやってるが、ほんとはスライダーのonValueChangedが呼ばれるはずなので、呼ばないでもOK
                _gamepadModelScaleTarget.localScale = gamepadModelScaleSlider.value * Vector3.one;
                
                //タイミングバグを踏むと嫌 + Setによって実際にレイアウトが変わるので、
                //「確かに受け取ったよ」という主旨で受信値をエコーバック
                _sender?.SendCommand(MessageFactory.Instance.UpdateDeviceLayout(data));
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }

            void ApplyItem(DeviceLayoutItem item, Transform target)
            {
                if (item == null)
                {
                    return;
                }

                target.position = item.pos;
                target.rotation = Quaternion.Euler(item.rot);
                target.localScale = item.scale;
            }
        }
        
        private void ResetDeviceLayout()
        {
            var parameters = _settingAutoAdjuster.GetDeviceLayoutParameters();
            FindObjectOfType<HidTransformController>().SetHidLayoutByParameter(parameters);
            FindObjectOfType<SmallGamepadProvider>().SetLayoutByParameter(parameters);
            //デバイス移動が入るので必ず送信
            SendDeviceLayoutData();
        }

        public void GamepadScaleChanged(float scale) 
            => _gamepadModelScaleTarget.localScale = scale * Vector3.one;

        //ラジオボタンのイベントハンドラっぽいやつ
        
        public void EnableLocalCoordinate(bool isOn)
            => UpdateSettingIfTrue(() => _preferWorldCoordinate = false, isOn);

        public void EnableWorldCoordinate(bool isOn)
            => UpdateSettingIfTrue(() => _preferWorldCoordinate = true, isOn);
        
        public void EnableTranslateMode(bool isOn)
            => UpdateSettingIfTrue(() => _mode = TransformControl.TransformMode.Translate, isOn);

        public void EnableRotateMode(bool isOn)
            => UpdateSettingIfTrue(() => _mode = TransformControl.TransformMode.Rotate, isOn);

        public void EnableScaleMode(bool isOn)
            => UpdateSettingIfTrue(() => _mode = TransformControl.TransformMode.Scale, isOn);

        private void UpdateSettingIfTrue(Action act, bool isOn)
        {
            if (!isOn)
            {
                return;
            }

            act();
            for (int i = 0; i < _transformControls.Length; i++)
            {
                _transformControls[i].global = _preferWorldCoordinate;
                _transformControls[i].mode = _mode;
            }
        }
    }
    
    [Serializable]
    public class DeviceLayoutsData
    {
        public DeviceLayoutItem keyboard;
        public DeviceLayoutItem touchPad;
        public DeviceLayoutItem midi;
        public DeviceLayoutItem gamepad;
        public float gamepadModelScale;
    }

    [Serializable]
    public class DeviceLayoutItem
    {
        public Vector3 pos;
        public Vector3 rot;
        public Vector3 scale;
    }
}
