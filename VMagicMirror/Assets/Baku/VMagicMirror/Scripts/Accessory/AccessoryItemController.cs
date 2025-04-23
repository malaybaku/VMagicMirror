using System;
using System.Collections.Generic;
using System.Linq;
using Baku.VMagicMirror.ExternalTracker;
using Baku.VMagicMirror.WordToMotion;
using UniRx;
using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    //TODO: このクラスでアクセサリ由来のメモリ消費量が爆発しないように工夫を入れる。
    // - アクセサリのenable on/offに応じてリソースの初期化 + 解放を行う
    // - 解放は何か一定のstableな戦略をとる。minimumでは「1回もactiveになってないうちはロードしない」とかでもいい
    //   - (連番pngについては何かうまい方法を考えていただけると…)

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
        private Animator _animator;
        private bool _hasModel;

        [Inject]
        public void Initialize(
            Camera cam,
            IVRMLoadable vrmLoader,
            IMessageReceiver receiver,
            IMessageSender sender, 
            FaceSwitchUpdater faceSwitchUpdater,
            DeviceTransformController deviceTransformController,
            WordToMotionAccessoryRequest accessoryRequest,
            BlinkTriggerDetector blinkTriggerDetector
            )
        {
            _cam = cam;
            _sender = sender;

            vrmLoader.VrmLoaded += info =>
            {
                _items.ForEach(i => i.SetAnimator(info.controlRig));
                _animator = info.controlRig;
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
                VmmCommands.ReloadAccessoryFiles,
                _ => ReloadAccessoryFiles()
                );
            receiver.AssignCommandHandler(
                VmmCommands.SetAccessoryLayout,
                c => SetAllAccessoryLayout(c.StringValue)
                );
            receiver.AssignCommandHandler(
                VmmCommands.SetSingleAccessoryLayout,
                c => SetSingleAccessoryLayout(c.StringValue)
                );
            receiver.AssignCommandHandler(
                VmmCommands.RequestResetAllAccessoryLayout,
                c => ResetAllAccessoryLayouts()
                );
            receiver.AssignCommandHandler(
                VmmCommands.RequestResetAccessoryLayout,
                c => ResetAccessoryLayout(c.StringValue)
                );

            faceSwitchUpdater.CurrentValue
                .Select(value => value.HasValue ? value.AccessoryName : "")
                .DistinctUntilChanged()
                .Subscribe(UpdateFaceSwitchStatus)
                .AddTo(this);

            deviceTransformController.ControlRequested
                .Subscribe(ControlItemsTransform)
                .AddTo(this);

            accessoryRequest.AccessoryRequest
                .Subscribe(UpdateWordToMotionStatus)
                .AddTo(this);

            blinkTriggerDetector.BlinkDetected
                .Subscribe(_ => FireBlinkTrigger())
                .AddTo(this);
        }

        private void UpdateFaceSwitchStatus(string fileId)
        {
            foreach (var item in _items)
            {
                item.VisibleByFaceSwitch = item.FileId == fileId;
            }
        }

        //NOTE: previewでも実際のモーションでも同じ所を叩かせる
        private void UpdateWordToMotionStatus(string fileId)
        {
            foreach (var item in _items)
            {
                item.VisibleByWordToMotion = item.FileId == fileId;
            }
        }

        private void FireBlinkTrigger()
        {
            foreach (var item in _items)
            {
                item.RunBlinkTrigger();
            }
        }

        private void Start()
        {
            ReloadAccessoryFiles();
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
                _items.ForEach(i => i.EndControlItemTransform());
            }
        }

        private void ReloadAccessoryFiles()
        {
            ClearItems();
            var files = AccessoryFile.LoadAccessoryFiles();
            foreach (var file in files)
            {
                var item = Instantiate(itemPrefab);
                item.Initialize(_cam, file);
                item.FirstEnabled += OnItemFirstEnabled;
                if (_hasModel)
                {
                    item.SetAnimator(_animator);
                }
                _items.Add(item);
            }
        }

        private void ClearItems()
        {
            var items = _items.ToArray();
            _items.Clear();
            foreach (var item in items)
            {
                item.FirstEnabled -= OnItemFirstEnabled;
                item.Dispose();
            }
        }

        private void OnItemFirstEnabled(AccessoryItem item)
        {
            item.LoadContent();
        }
        
        private void SetSingleAccessoryLayout(string json)
        {
            try
            {
                var decoded = JsonUtility.FromJson<AccessoryItemLayout>(json);
                if (string.IsNullOrEmpty(decoded.FileId))
                {
                    return;
                }
                
                if (_items.FirstOrDefault(i => i.FileId == decoded.FileId) is { } item)
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
                foreach (var layout in layouts.Items.Where(l => !string.IsNullOrEmpty(l.FileId)))
                {
                    if (_items.FirstOrDefault(i => i.FileId == layout.FileId) is { } item)
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
                foreach (var file in files.FileIds)
                {
                    if (_items.FirstOrDefault(i => i.FileId == file) is { } item)
                    {
                        item.ResetLayout();
                    }
                }
                SendLayout();
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

            var msg = JsonUtility.ToJson(layouts);
            _sender.SendCommand(MessageFactory.Instance.UpdateAccessoryLayouts(msg));
        }
    }
}
