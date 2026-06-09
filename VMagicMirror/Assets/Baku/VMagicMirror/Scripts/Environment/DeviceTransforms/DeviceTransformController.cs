using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using mattatz.TransformControl;
using R3;

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
        
        [SerializeField] private DeviceTransformControlCanvas canvasPrefab;

        public Slider GamepadModelScaleSlider { get; set; }
        public Canvas RawCanvas { get; set; }
        
        private IMessageSender _sender;
        
        private bool _hasCanvas;
        private float _gamepadModelScale = 1.0f;
        
        private SettingAutoAdjuster _settingAutoAdjuster;
        private RuntimeTransformControlFactory _transformControlFactory;
        private HidTransformController _hidTransformController;
        private GamepadProvider _gamepad;
        private ArcadeStickProvider _arcadeStick;
        private CarHandleProvider _carHandle;
        private PenTabletProvider _penTable;

        private Transform _keyboardTransform;
        private Transform _touchPadTransform;
        private Transform _midiTransform;
        private Transform _gamepadTransform;
        private Transform _arcadeStickTransform;
        private Transform _carHandleTransform;
        private Transform _penTabletTransform;

        private RuntimeTransformControlHandle _keyboardControl;
        private RuntimeTransformControlHandle _touchPadControl;
        private RuntimeTransformControlHandle _midiControl;
        private RuntimeTransformControlHandle _gamepadControl;
        private RuntimeTransformControlHandle _arcadeStickControl;
        private RuntimeTransformControlHandle _carHandleControl;
        private RuntimeTransformControlHandle _penTabletControl;
        private Transform _gamepadModelScaleTarget;
        
        private bool _preferWorldCoordinate;
        private TransformControl.TransformMode _mode = TransformControl.TransformMode.Translate;

        private RuntimeTransformControlHandle[] _transformControls;
        private RuntimeTransformControlHandle[] TransformControls => _transformControls ??= new[]
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
        public Observable<TransformControlRequest> ControlRequested => _controlRequested;

        private KeyboardVisibilityView _keyboardVisibility;
        private TouchpadVisibilityView _touchPadVisibility;
        private GamepadVisibilityView _gamepadVisibility;
        private ArcadeStickVisibilityView _arcadeStickVisibility;
        private CarHandleVisibilityView _carHandleVisibility;
        private PenTabletVisibilityView _penTabletVisibility;
        private MidiControllerVisibility _midiControllerVisibility;
                
        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            IMessageSender sender,
            SettingAutoAdjuster settingAutoAdjuster,
            RuntimeTransformControlFactory transformControlFactory,
            HidTransformController hidTransformController,
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
            _transformControlFactory = transformControlFactory;

            _hidTransformController = hidTransformController;
            _gamepad = gamepad;
            _arcadeStick = arcadeStick;
            _carHandle = carHandle;
            _penTable = penTablet;

            _keyboardTransform = keyboard.transform;
            _touchPadTransform = touchPad.transform;
            _midiTransform = midiController.transform;
            _gamepadTransform = gamepad.transform;
            _arcadeStickTransform = arcadeStick.transform;
            _carHandleTransform = carHandle.transform;
            _penTabletTransform = penTablet.transform;
            _gamepadModelScaleTarget = gamepad.ModelScaleTarget;

            _transformControlFactory.DisableExisting(_keyboardTransform);
            _transformControlFactory.DisableExisting(_touchPadTransform);
            _transformControlFactory.DisableExisting(_midiTransform);
            _transformControlFactory.DisableExisting(_gamepadTransform);
            _transformControlFactory.DisableExisting(_arcadeStickTransform);
            _transformControlFactory.DisableExisting(_carHandleTransform);
            _transformControlFactory.DisableExisting(_penTabletTransform);
            
            _keyboardVisibility = keyboard.GetComponent<KeyboardVisibilityView>();
            _touchPadVisibility =  touchPad.GetComponent<TouchpadVisibilityView>();
            _gamepadVisibility = gamepad.GetComponent<GamepadVisibilityView>();
            _arcadeStickVisibility = arcadeStick.GetComponent<ArcadeStickVisibilityView>();
            _carHandleVisibility = carHandle.GetComponent<CarHandleVisibilityView>();
            _midiControllerVisibility = midiController.GetComponent<MidiControllerVisibility>();
            _penTabletVisibility = penTablet.GetComponent<PenTabletVisibilityView>();
            
            receiver.AssignCommandHandler(
                VmmCommands.EnableDeviceFreeLayout,
                command => EnableDeviceFreeLayout(command.ToBoolean())
            );
            receiver.AssignCommandHandler(
                VmmCommands.SetDeviceLayout,
                command => SetDeviceLayout(command.GetStringValue())
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

            EnsureTransformControls();

            _keyboardControl.Control.mode = _keyboardVisibility.IsVisible ? _mode : TransformControl.TransformMode.None;
            _touchPadControl.Control.mode = _touchPadVisibility.IsVisible ? _mode : TransformControl.TransformMode.None;
            _gamepadControl.Control.mode = _gamepadVisibility.IsVisible ? _mode : TransformControl.TransformMode.None;
            //NOTE:
            // アケコンは実機スケールを重んじるため、スケール変化は認めない
            // 車のハンドルも物理ベースで同様に考えうるが、ハンドルについてはスケールがxyzで歪まないことだけ保証している
            _arcadeStickControl.Control.mode = _arcadeStickVisibility.IsVisible && _mode != TransformControl.TransformMode.Scale
                ? _mode
                : TransformControl.TransformMode.None;
            _carHandleControl.Control.mode = _carHandleVisibility.IsVisible ? _mode : TransformControl.TransformMode.None;
            _midiControl.Control.mode = _midiControllerVisibility.IsVisible ? _mode : TransformControl.TransformMode.None;
            _penTabletControl.Control.mode = _penTabletVisibility.IsVisible ? _mode : TransformControl.TransformMode.None;

            foreach (var transformControl in TransformControls)
            {
                transformControl.Control.Control();
            }
            
            AdjustCarHandleScale();
            
            _controlRequested.OnNext(new TransformControlRequest(_preferWorldCoordinate, _mode));
        }

        //CarHandleのscaleのx,y,z成分が等しい状態にする
        private void AdjustCarHandleScale()
        {
            if (_carHandleControl?.Control == null ||
                _carHandleControl.Control.mode != TransformControl.TransformMode.Scale)
            {
                return;
            }

            const float ScaleDiffThreshold = 0.0001f;

            //scaleに仲間外れの値がある場合、その値が編集されたと見なして他2つの値を追従させる
            var t = _carHandleTransform;
            
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

            if (enable)
            {
                EnsureTransformControls();
                foreach (var transformControl in TransformControls)
                {
                    transformControl.Control.global = _preferWorldCoordinate;
                    transformControl.Control.mode = _mode;
                }
            }
            else
            {
                ReleaseTransformControls();
            }
        }

        private void SendDeviceLayoutData()
        {
            var data = new DeviceLayoutsData()
            {
                keyboard = ToItem(_keyboardTransform),
                touchPad = ToItem(_touchPadTransform),
                midi = ToItem(_midiTransform),
                gamepad = ToItem(_gamepadTransform),
                arcadeStick = ToItem(_arcadeStickTransform),
                carHandle = ToItem(_carHandleTransform),
                penTablet = ToItem(_penTabletTransform),
                gamepadModelScale = _gamepadModelScaleTarget.localScale.x,
            };
            _sender?.SendCommand(MessageFactory.UpdateDeviceLayout(data));

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
                // TODO: Transform編集に関するコードのトレーサビリティが悪いのを直したい
                // 具体的には、各ControlなりProviderなりのクラスからSetPosition系のメソッドが生えてると嬉しい
                var data = JsonUtility.FromJson<DeviceLayoutsData>(content);
                ApplyItem(data.keyboard, _keyboardTransform);
                ApplyItem(data.touchPad, _touchPadTransform);
                ApplyItem(data.midi, _midiTransform);
                ApplyItem(data.gamepad, _gamepadTransform);
                ApplyItem(data.arcadeStick, _arcadeStickTransform);
                ApplyItem(data.carHandle, _carHandleTransform);
                ApplyItem(data.penTablet, _penTabletTransform);

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
                _sender?.SendCommand(MessageFactory.UpdateDeviceLayout(data));
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

            _hidTransformController.SetHidLayoutByParameter(parameters);
            _gamepad.SetLayoutByParameter(parameters);
            _arcadeStick.SetLayoutByParameter(parameters);
            _penTable.SetLayoutParameter(parameters);
            _carHandle.SetLayoutParameter(parameters);
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

            _hidTransformController.SetHidLayoutByParameter(parameters);
            _gamepad.SetLayoutByParameter(parameters);
            _arcadeStick.SetLayoutByParameter(parameters);
            _penTable.SetLayoutParameter(parameters);
            _carHandle.SetLayoutParameter(parameters);
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
            if (!IsDeviceFreeLayoutEnabled)
            {
                return;
            }

            EnsureTransformControls();
            foreach (var transformControl in TransformControls)
            {
                transformControl.Control.global = _preferWorldCoordinate;
                transformControl.Control.mode = _mode;
            }
        }

        private void EnsureTransformControls()
        {
            _keyboardControl ??= _transformControlFactory.Create(_keyboardTransform);
            _touchPadControl ??= _transformControlFactory.Create(_touchPadTransform);
            _midiControl ??= _transformControlFactory.Create(_midiTransform);
            _gamepadControl ??= _transformControlFactory.Create(_gamepadTransform);
            _arcadeStickControl ??= _transformControlFactory.Create(_arcadeStickTransform);
            _carHandleControl ??= _transformControlFactory.Create(_carHandleTransform);
            _penTabletControl ??= _transformControlFactory.Create(_penTabletTransform);

            _transformControls = new[]
            {
                _keyboardControl,
                _touchPadControl,
                _midiControl,
                _gamepadControl,
                _arcadeStickControl,
                _carHandleControl,
                _penTabletControl,
            };
        }

        private void ReleaseTransformControls()
        {
            foreach (var transformControl in TransformControls)
            {
                transformControl?.Dispose();
            }

            _keyboardControl = null;
            _touchPadControl = null;
            _midiControl = null;
            _gamepadControl = null;
            _arcadeStickControl = null;
            _carHandleControl = null;
            _penTabletControl = null;
            _transformControls = null;
        }

        private void OnDestroy() => ReleaseTransformControls();
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
