using System;
using UnityEngine;
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

        [SerializeField] private TransformControl[] transformControls = null;

        private bool _isDeviceFreeLayoutEnabled = false;
        private bool _preferWorldCoordinate = false;
        private TransformControl.TransformMode _mode = TransformControl.TransformMode.Translate;
        private Canvas _canvas = null;
        
        private void Start()
        {
            _canvas = GetComponent<Canvas>();
            _handler.Commands.Subscribe(command =>
            {
                if (command.Command == MessageCommandNames.EnableDeviceFreeLayout)
                {
                    EnableDeviceFreeLayout(command.ToBoolean());
                }
            });
        }

        private void Update()
        {
            if (_isDeviceFreeLayoutEnabled)
            {
                for (int i = 0; i < transformControls.Length; i++)
                {
                    transformControls[i].Control();
                }
            }
        }

        private void EnableDeviceFreeLayout(bool enable)
        {
            Debug.Log("Enable Device Free Layout: " + enable);
            if (_isDeviceFreeLayoutEnabled == enable)
            {
                return;
            }
            
            _isDeviceFreeLayoutEnabled = enable;
            _canvas.enabled = enable;
            for (int i = 0; i < transformControls.Length; i++)
            {
                transformControls[i].enabled = enable;
                transformControls[i].mode = enable ? _mode : TransformControl.TransformMode.None;
            }
        }

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
            for (int i = 0; i < transformControls.Length; i++)
            {
                transformControls[i].global = _preferWorldCoordinate;
                transformControls[i].mode = _mode;
            }
        }
    }
}
