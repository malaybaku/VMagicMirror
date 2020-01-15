using System;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
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
        [Inject] private ReceivedMessageHandler _handler;

        //NOTE: このクラスでanimatorとかを直読みしたくないので、リセット処理を外注します
        [SerializeField] private SettingAutoAdjuster settingAutoAdjuster = null;

        [SerializeField] private TransformControl keyboardControl = null;
        [SerializeField] private TransformControl touchPadControl = null;
        [SerializeField] private TransformControl midiControl = null;
        [SerializeField] private TransformControl gamepadControl= null;
        [SerializeField] private Transform gamepadModelScaleTarget = null;
        [SerializeField] private Slider gamepadModelScaleSlider = null;

        public TransformControl KeyboardControl => keyboardControl;
        public TransformControl TouchPadControl => touchPadControl;
        public TransformControl GamepadControl => gamepadControl;
        public TransformControl MidiControl => midiControl;

        public bool CanShowKeyboardControl { get; set; } = true;
        public bool CanShowTouchpadControl { get; set; } = true;
        public bool CanShowGamepadControl { get; set; } = true;
        public bool CanShowMidiControl { get; set; } = true;

        private TransformControl[] _transformControls => new[]
        {
            keyboardControl,
            touchPadControl,
            midiControl,
            gamepadControl,
        };

        public bool IsDeviceFreeLayoutEnabled { get; private set; }
        
        private bool _preferWorldCoordinate = false;
        private TransformControl.TransformMode _mode = TransformControl.TransformMode.Translate;
        private Canvas _canvas = null;
        
        private void Start()
        {
            _canvas = GetComponent<Canvas>();
            _handler.Commands.Subscribe(command =>
            {
                switch (command.Command)
                {
                    case MessageCommandNames.EnableDeviceFreeLayout:
                        EnableDeviceFreeLayout(command.ToBoolean());
                        break;
                    case MessageCommandNames.SetDeviceLayout:
                        SetDeviceLayout(command.Content);
                        break;
                    case MessageCommandNames.ResetDeviceLayout:
                        ResetDeviceLayout();
                        break;
                }
            });

            _handler.QueryRequested += query =>
            {
                if (query.Command == MessageQueryNames.CurrentDeviceLayout)
                {
                    query.Result = GetDeviceLayouts(); 
                }
            };
        }

        private void Update()
        {
            if (!IsDeviceFreeLayoutEnabled)
            {
                return;
            }

            keyboardControl.mode = CanShowKeyboardControl ? _mode : TransformControl.TransformMode.None;
            touchPadControl.mode = CanShowTouchpadControl ? _mode : TransformControl.TransformMode.None;
            gamepadControl.mode = CanShowGamepadControl ? _mode : TransformControl.TransformMode.None;
            midiControl.mode = CanShowMidiControl ? _mode : TransformControl.TransformMode.None;
            
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

        private string GetDeviceLayouts()
        {
            return JsonUtility.ToJson(new DeviceLayoutsData()
            {
                keyboard = ToItem(keyboardControl.transform),
                touchPad = ToItem(touchPadControl.transform),
                midi = ToItem(midiControl.transform),
                gamepad =  ToItem(gamepadControl.transform),
                gamepadModelScale = gamepadModelScaleTarget.localScale.x,
            });

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
                ApplyItem(data.keyboard, keyboardControl.transform);
                ApplyItem(data.touchPad, touchPadControl.transform);
                ApplyItem(data.midi, midiControl.transform);
                ApplyItem(data.gamepad, gamepadControl.transform);
                gamepadModelScaleSlider.value = Mathf.Clamp(
                    data.gamepadModelScale,
                    gamepadModelScaleSlider.minValue,
                    gamepadModelScaleSlider.maxValue);
                //NOTE: ここは念押しでやってるが、ほんとはスライダーのonValueChangedが呼ばれるはずなので、呼ばないでもOK
                gamepadModelScaleTarget.localScale = gamepadModelScaleSlider.value * Vector3.one;
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
            var parameters = settingAutoAdjuster.GetDeviceLayoutParameters();
            FindObjectOfType<HidTransformController>().SetHidLayoutByParameter(parameters);
            FindObjectOfType<SmallGamepadProvider>().SetLayoutByParameter(parameters);
        }

        public void GamepadScaleChanged(float scale) 
            => gamepadModelScaleTarget.localScale = scale * Vector3.one;

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
