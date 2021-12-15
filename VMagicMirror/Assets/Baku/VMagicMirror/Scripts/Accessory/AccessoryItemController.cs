using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// アクセサリー機能の管理としては一番えらいクラス
    /// </summary>
    public class AccessoryItemController : MonoBehaviour
    {
        [SerializeField] private AccessoryItem itemPrefab = null;

        private Camera _cam;
        private IMessageSender _sender;
        private readonly List<AccessoryItem> _items = new List<AccessoryItem>();
        private IDisposable _layoutSender = null;
        private bool _hasModel;

        [Inject]
        public void Initialize(
            Camera cam,
            IVRMLoadable vrmLoader,
            IMessageReceiver receiver,
            IMessageSender sender, 
            DeviceTransformController deviceTransformController
            )
        {
            _cam = cam;
            _sender = sender;

            vrmLoader.VrmLoaded += info =>
            {
                _items.ForEach(i => i.SetModel(info));
                _hasModel = true;
            };
            vrmLoader.VrmDisposing += () =>
            {
                _hasModel = false;
                _items.ForEach(i => i.UnsetModel());
            };
            
            receiver.AssignCommandHandler(
                VmmCommands.EnableDeviceFreeLayout,
                c => SetFreeLayout(c.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.SetAccessoryLayout,
                c => SetAllAccessoryLayout(c.Content)
                );
            receiver.AssignCommandHandler(
                VmmCommands.SetSingleAccessoryLayout,
                c => SetSingleAccessoryLayout(c.Content)
                );
            receiver.AssignCommandHandler(
                VmmCommands.RequestResetAllAccessoryLayout,
                c => ResetAllAccessoryLayouts()
                );
            receiver.AssignCommandHandler(
                VmmCommands.RequestResetAccessoryLayout,
                c => ResetAccessoryLayout(c.Content));

            deviceTransformController.ControlRequested
                .Subscribe(ControlItemsTransform)
                .AddTo(this);
        }

        private void Start()
        {
            var files = AccessoryFile.LoadAccessoryFiles();
            foreach (var file in files)
            {
                var item = Instantiate(itemPrefab);
                item.Initialize(_cam, file);
                _items.Add(item);
            }
        }

        private void OnDestroy()
        {
            StopLayoutSending();
        }

        private void SetFreeLayout(bool enable)
        {
            if (enable)
            {
                StartLayoutSending();
            }
            else
            {
                StopLayoutSending();
                SendLayout();
            }
        }
        
        private void SetSingleAccessoryLayout(string json)
        {
            try
            {
                var decoded = JsonUtility.FromJson<AccessoryItemLayout>(json);
                if (string.IsNullOrEmpty(decoded.FileName))
                {
                    return;
                }
                
                if (_items.FirstOrDefault(i => i.FileName == decoded.FileName) is { } item)
                {
                    item.SetLayout(decoded);
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void SetAllAccessoryLayout(string json)
        {
            try
            {
                var layouts = JsonUtility.FromJson<AccessoryLayouts>(json);
                foreach (var layout in layouts.Items.Where(l => !string.IsNullOrEmpty(l.FileName)))
                {
                    if (_items.FirstOrDefault(i => i.FileName == layout.FileName) is { } item)
                    {
                        item.SetLayout(layout);
                    }
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
        
        private void ResetAccessoryLayout(string fileNamesJson)
        {
            try
            {
                var files = JsonUtility.FromJson<AccessoryResetTargetItems>(fileNamesJson);
                foreach (var file in files.FileNames)
                {
                    if (_items.FirstOrDefault(i => i.FileName == file) is { } item)
                    {
                        item.ResetLayout();
                    }
                    SendLayout();
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }

        private void ResetAllAccessoryLayouts()
        {
            foreach (var item in _items)
            {
                item.ResetLayout();
            }
            SendLayout();
        }
        
        private void ControlItemsTransform(TransformControlRequest request)
        {
            foreach (var item in _items)
            {
                item.ControlItemTransform(request);
            }
        }

        private void StartLayoutSending()
        {
            _layoutSender?.Dispose();
            _layoutSender = Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(_ =>
                {
                    if (_hasModel)
                    {
                        SendLayout();
                    }
                });
        }

        private void StopLayoutSending()
        {
            _layoutSender?.Dispose();
            _layoutSender = null;
        }
        
        //Unity側で操作のあったアクセサリのレイアウト情報を送信する
        private void SendLayout()
        {
            var items = new List<AccessoryItemLayout>();
            foreach (var target in _items.Where(i => i.HasLayoutChange))
            {
                items.Add(target.ItemLayout);
                target.ConfirmLayoutChange();
            }
            
            var layouts = new AccessoryLayouts()
            {
                Items = items.ToArray(),
            };
            
            _sender.SendCommand(MessageFactory.Instance.UpdateAccessoryLayouts(
                JsonUtility.ToJson(layouts)
            ));
        }
    }
}
