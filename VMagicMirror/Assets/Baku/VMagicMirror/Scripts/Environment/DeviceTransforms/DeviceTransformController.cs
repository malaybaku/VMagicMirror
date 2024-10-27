using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using mattatz.TransformControl;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// <see cref="DeviceTransformController"/>の外部でTransformControlを表示したい人に対して送るデータ
    /// </summary>
    public readonly struct TransformControlRequest
    {
        public TransformControlRequest(bool worldCoordinate, TransformControl.TransformMode mode)
        {
            WorldCoordinate = worldCoordinate;
            Mode = mode;
        }
        public readonly bool WorldCoordinate;
        public readonly TransformControl.TransformMode Mode;
    }

    /// <summary>
    /// キーボードやマウスパッドの位置をユーザーが自由に編集できるかどうかを設定するレシーバークラス
    /// UIが必要になるので、そのUIの操作もついでにここでやります
    /// </summary>
    public class DeviceTransformController : MonoBehaviour
    {
        private const float GamePadModelMinScale = 0.1f;
        private const float GamePadModelMaxScale = 3.0f;
        
        [SerializeField] private DeviceTransformControlCanvas canvasPrefab = null;

        public Slider GamepadModelScaleSlider { get; set; }
        public Canvas RawCanvas { get; set; }
        
        private IMessageSender _sender;
        
        private bool _hasCanvas = false;
        private float _gamepadModelScale = 1.0f;
        
        private SettingAutoAdjuster _settingAutoAdjuster = null;
        private TransformControl _keyboardControl = null;
        private TransformControl _touchPadControl = null;
        private TransformControl _midiControl = null;
        private TransformControl _gamepadControl= null;
        private TransformControl _arcadeStickControl= null;
        private TransformControl _carHandleControl = null;
        private TransformControl _penTabletControl = null;
        private Transform _gamepadModelScaleTarget = null;
        
        private bool _preferWorldCoordinate = false;
        private TransformControl.TransformMode _mode = TransformControl.TransformMode.Translate;

        private TransformControl[] _transformControls => new[]
        {
            _keyboardControl,
            _touchPadControl,
            _midiControl,
            _gamepadControl,
            _arcadeStickControl,
            _carHandleControl,
            _penTabletControl,
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

        private readonly Subject<TransformControlRequest> _controlRequested = new Subject<TransformControlRequest>();
        /// <summary>
        /// フリーレイアウトが有効なあいだ、レイアウトの操作対象の情報を載せて毎フレーム送信される値。
        /// フリーレイアウトが無効になると送信されなくなることに注意
        /// </summary>
        public IObservable<TransformControlRequest> ControlRequested => _controlRequested;

        private KeyboardVisibility _keyboardVisibility;
        private TouchpadVisibility _touchPadVisibility;
        private GamepadVisibilityView _gamepadVisibility;
        private ArcadeStickVisibilityView _arcadeStickVisibility;
        private CarHandleVisibilityView _carHandleVisibility;
        private PenTabletVisibility _penTabletVisibility;
        private MidiControllerVisibility _midiControllerVisibility;
                
        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            IMessageSender sender,
            SettingAutoAdjuster settingAutoAdjuster,
            KeyboardProvider keyboard,
            TouchPadProvider touchPad,
            MidiControllerProvider midiController,
            GamepadProvider gamepad,
            ArcadeStickProvider arcadeStick,
            CarHandleProvider carHandle,
            PenTabletProvider penTablet
        )
        {
            _sender = sender;
            _settingAutoAdjuster = settingAutoAdjuster;

            _keyboardControl = keyboard.TransformControl;
            _touchPadControl = touchPad.TransformControl;
            _midiControl = midiController.TransformControl;
            _gamepadControl = gamepad.TransformControl;
            _arcadeStickControl = arcadeStick.TransformControl;
            _carHandleControl = carHandle.TransformControl;
            _penTabletControl = penTablet.TransformControl;
            _gamepadModelScaleTarget = gamepad.ModelScaleTarget;
            
            _keyboardVisibility = _keyboardControl.GetComponent<KeyboardVisibility>();
            _touchPadVisibility =  _touchPadControl.GetComponent<TouchpadVisibility>();
            _gamepadVisibility = _gamepadControl.GetComponent<GamepadVisibilityView>();
            _arcadeStickVisibility = _arcadeStickControl.GetComponent<ArcadeStickVisibilityView>();
            _carHandleVisibility = _carHandleControl.GetComponent<CarHandleVisibilityView>();
            _midiControllerVisibility = _midiControl.GetComponent<MidiControllerVisibility>();
            _penTabletVisibility = _penTabletControl.GetComponent<PenTabletVisibility>();
            
            receiver.AssignCommandHandler(
                VmmCommands.EnableDeviceFreeLayout,
                command => EnableDeviceFreeLayout(command.ToBoolean())
            );
            receiver.AssignCommandHandler(
                VmmCommands.SetDeviceLayout,
                command => SetDeviceLayout(command.Content)
            );
            receiver.AssignCommandHandler(
                VmmCommands.ResetDeviceLayout,
                command => ResetDeviceLayout()
            );
        }

        private void Update()
        {
            if (!IsDeviceFreeLayoutEnabled)
            {
                return;
            }

            _keyboardControl.mode = _keyboardVisibility.IsVisible ? _mode : TransformControl.TransformMode.None;
            _touchPadControl.mode = _touchPadVisibility.IsVisible ? _mode : TransformControl.TransformMode.None;
            _gamepadControl.mode = _gamepadVisibility.IsVisible ? _mode : TransformControl.TransformMode.None;
            //NOTE:
            // アケコンは実機スケールを重んじるため、スケール変化は認めない
            // 車のハンドルも物理ベースで同様に考えうるが、ハンドルについてはスケールがxyzで歪まないことだけ保証している
            _arcadeStickControl.mode = _arcadeStickVisibility.IsVisible && _mode != TransformControl.TransformMode.Scale 
                ? _mode
                : TransformControl.TransformMode.None;
            _carHandleControl.mode = _carHandleVisibility.IsVisible ? _mode : TransformControl.TransformMode.None;
            _midiControl.mode = _midiControllerVisibility.IsVisible ? _mode : TransformControl.TransformMode.None;
            _penTabletControl.mode = _penTabletVisibility.IsVisible ? _mode : TransformControl.TransformMode.None;

            for (int i = 0; i < _transformControls.Length; i++)
            {
                _transformControls[i].Control();
            }
            
            AdjustCarHandleScale();
            
            _controlRequested.OnNext(new TransformControlRequest(_preferWorldCoordinate, _mode));
        }

        //CarHandleのscaleのx,y,z成分が等しい状態にする
        private void AdjustCarHandleScale()
        {
            if (_carHandleControl.mode != TransformControl.TransformMode.Scale)
            {
                return;
            }

            const float ScaleDiffThreshold = 0.0001f;

            //scaleに仲間外れの値がある場合、その値が編集されたと見なして他2つの値を追従させる
            var t = _carHandleControl.transform;
            
            var scale = t.localScale;

            var xy = Mathf.Abs(scale.x - scale.y) > ScaleDiffThreshold;
            var yz = Mathf.Abs(scale.y - scale.z) > ScaleDiffThreshold;
            var zx = Mathf.Abs(scale.z - scale.x) > ScaleDiffThreshold;
            if (!xy && !yz && !zx)
            {
                //このフレームでscaleが変動してない限りはここを通過する
                return;
            }

            //どこかのスケールが変化した場合
            if (!xy)
            {
                //xyが等しい -> zが仲間はずれなのでzの値を採用する。以降の分岐も同じ考え方による
                t.localScale = Vector3.one * scale.z;
            }
            else if (!yz)
            {
                t.localScale = Vector3.one * scale.x;
            }
            else
            {
                t.localScale = Vector3.one * scale.y;
            }
        }
        
        private void CreateCanvasIfNotExist()
        {
            if (!_hasCanvas)
            {
                var canvas = Instantiate(canvasPrefab, transform);
                canvas.Connect(this);
                GamepadModelScaleSlider.value = _gamepadModelScale;
                _hasCanvas = true;
            }
        }
        
        private void EnableDeviceFreeLayout(bool enable)
        {
            if (IsDeviceFreeLayoutEnabled == enable)
            {
                return;
            }
            
            IsDeviceFreeLayoutEnabled = enable;

            //NOTE: 1回もUIを作ってないときに非表示指定をされたとき、わざわざInstantiateをしなくてもいいよね、という主旨のガード
            if (_hasCanvas || enable)
            {
                CreateCanvasIfNotExist();
            }

            if (_hasCanvas)
            {
                RawCanvas.gameObject.SetActive(IsDeviceFreeLayoutEnabled);
            }

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
                gamepad = ToItem(_gamepadControl.transform),
                arcadeStick = ToItem(_arcadeStickControl.transform),
                carHandle = ToItem(_carHandleControl.transform),
                penTablet = ToItem(_penTabletControl.transform),
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
            if (string.IsNullOrEmpty(content))
            {
                SetInitialDeviceLayout();
                return;
            }
        
            try
            {
                var data = JsonUtility.FromJson<DeviceLayoutsData>(content);
                ApplyItem(data.keyboard, _keyboardControl.transform);
                ApplyItem(data.touchPad, _touchPadControl.transform);
                ApplyItem(data.midi, _midiControl.transform);
                ApplyItem(data.gamepad, _gamepadControl.transform);
                ApplyItem(data.arcadeStick, _arcadeStickControl.transform);
                ApplyItem(data.carHandle, _carHandleControl.transform);
                ApplyItem(data.penTablet, _penTabletControl.transform);

                _gamepadModelScale = Mathf.Clamp(
                    data.gamepadModelScale,
                    GamePadModelMinScale,
                    GamePadModelMaxScale
                );
                
                _gamepadModelScaleTarget.localScale = _gamepadModelScale * Vector3.one;
                if (_hasCanvas)
                {
                    GamepadModelScaleSlider.value = _gamepadModelScale;
                }
                
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
                //NOTE: 2つ目/3つ目の条件によって、
                //「設定ファイルに何もなかったから原点姿勢/ゼロスケール扱いにしたよ」
                //という(余計なお世話の)ケースを拒否する。
                //これはファイルのセーブタイミングがまずかったり、異バージョンの設定をロードすると起こるケース
                if (item == null || item.pos.magnitude < 0.01f || item.scale.magnitude < 0.01f) 
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
            //NOTE: キャラロードしてないとnullになることがあるのでガード
            var parameters = 
                _settingAutoAdjuster.GetDeviceLayoutParameters() ??
                new DeviceLayoutAutoAdjustParameters()
                {
                    ArmLengthFactor = 1.0f,
                    HeightFactor = 1.0f,
                };
            
            FindObjectOfType<HidTransformController>().SetHidLayoutByParameter(parameters);
            FindObjectOfType<GamepadProvider>().SetLayoutByParameter(parameters);
            FindObjectOfType<ArcadeStickProvider>().SetLayoutByParameter(parameters);
            FindObjectOfType<PenTabletProvider>().SetLayoutParameter(parameters);
            FindObjectOfType<CarHandleProvider>().SetLayoutParameter(parameters);
            //デバイス移動が入るので必ず送信
            SendDeviceLayoutData();
        }

        //NOTE: これが呼ばれるケースはかなりレアで、
        //「デバイスレイアウトの変更後、初期レイアウトのままでセーブしたデータをロードした」時だけ呼ばれる
        private void SetInitialDeviceLayout()
        {
            var parameters = new DeviceLayoutAutoAdjustParameters()
            {
                ArmLengthFactor = 1.0f,
                HeightFactor = 1.0f,
            };
            FindObjectOfType<HidTransformController>().SetHidLayoutByParameter(parameters);
            FindObjectOfType<GamepadProvider>().SetLayoutByParameter(parameters);
            FindObjectOfType<ArcadeStickProvider>().SetLayoutByParameter(parameters);
            FindObjectOfType<PenTabletProvider>().SetLayoutParameter(parameters);
            FindObjectOfType<CarHandleProvider>().SetLayoutParameter(parameters);
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
        public DeviceLayoutItem arcadeStick;
        public DeviceLayoutItem carHandle;
        public DeviceLayoutItem penTablet;
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
