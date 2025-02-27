using System;
using System.Collections.Generic;
using Baku.VMagicMirror.Buddy.Api;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary> WPFからBuddyのプロパティ情報を受けてインスタンス生成とか設定の保存とかをするクラス </summary>
    public class BuddyLayoutUpdater : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly IVRMLoadable _vrmLoadable;
        private readonly ScriptLoader _scriptLoader;
        private readonly BuddySpriteCanvas _spriteCanvas;
        private readonly BuddyLayoutRepository _buddyLayoutRepository;
        private readonly BuddyTransformInstanceRepository _transformInstanceRepository;
        private readonly Buddy3DInstanceCreator _buddy3DInstanceCreator;
        private readonly DeviceTransformController _deviceTransformController;
        
        private ReactiveProperty<bool> _freeLayoutEnabled = new();
        private bool _hasModel;
        private Animator _animator;
        
        // NOTE: インスタンスのキャッシュは誰が持つ？
        //このクラスで持つならクラス名がUpdaterじゃなさそうだが

        public BuddyLayoutUpdater(
            IMessageReceiver receiver, 
            IVRMLoadable vrmLoadable,
            ScriptLoader scriptLoader,
            BuddySpriteCanvas spriteCanvas,
            Buddy3DInstanceCreator buddy3DInstanceCreator,
            BuddyLayoutRepository buddyLayoutRepository,
            BuddyTransformInstanceRepository transformInstanceRepository,
            DeviceTransformController deviceTransformController)
        {
            _receiver = receiver;
            _vrmLoadable = vrmLoadable;
            _scriptLoader = scriptLoader;
            _spriteCanvas = spriteCanvas;
            _buddy3DInstanceCreator = buddy3DInstanceCreator;
            _buddyLayoutRepository = buddyLayoutRepository;
            _transformInstanceRepository = transformInstanceRepository;
            _deviceTransformController = deviceTransformController;
        }

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;

            _receiver.AssignCommandHandler(
                VmmCommands.BuddyRefreshData,
                c => RefreshBuddyLayout(c.Content)
            );

            _receiver.AssignCommandHandler(
                VmmCommands.BuddySetProperty,
                c => SetBuddyLayout(c.Content)
            );

            _receiver.BindBoolProperty(VmmCommands.EnableDeviceFreeLayout, _freeLayoutEnabled);

            _freeLayoutEnabled
                .Subscribe(v =>
                {
                    if (!v)
                    {
                        ResetTransformControlMode();
                    }
                })
                .AddTo(this);
            
            // NOTE: スクリプトのロード時点でLayout情報は既に手元にあるのが前提になっている
            _scriptLoader.ScriptLoading
                .Subscribe(CreateTransformInstance)
                .AddTo(this);
            _scriptLoader.ScriptDisposing
                .Subscribe(DeleteTransformInstance)
                .AddTo(this);

            _deviceTransformController.ControlRequested
                .Subscribe(ControlTransform3Ds)
                .AddTo(this);
        }


        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            // NOTE: ロード済みのインスタンスで、VRMのボーンにアタッチしたいようなものがあれば実際にアタッチする…というのをやっている
            foreach (var transform in _transformInstanceRepository.GetTransform3DInstances())
            {
                if (!_buddyLayoutRepository
                    .Get(transform.BuddyId)
                    .Transform3Ds
                    .TryGetValue(transform.InstanceName, out var layout))
                {
                    continue;
                }

                if (!layout.HasParentBone)
                {
                    continue;
                }

                var parentBone = info.animator.GetBoneTransformAscending(layout.ParentBone);
                transform.SetParent(parentBone);
            }

            _animator = info.animator;
            _hasModel = true;
        }

        private void OnVrmDisposing()
        {
            _hasModel = false;
            _animator = null;
            
            foreach (var transform in _transformInstanceRepository.GetTransform3DInstances())
            {
                transform.RemoveParent();
            }
        }

        private void SetBuddyLayout(string json)
        {
            try
            {
                var msg = JsonUtility.FromJson<BuddySettingsPropertyMessage>(json);
                
                // やることは2つ。
                // - 設定のレポジトリに値を入れておく
                // - インスタンスがある場合、そのインスタンスに値を適用する
                if (TryGetBuddyTransform2DLayout(msg, out var layout2d))
                {
                    _buddyLayoutRepository.Get(msg.BuddyId).AddOrUpdate(msg.Name, layout2d);
                    if (_transformInstanceRepository.TryGetTransform2D(msg.BuddyId, msg.Name, out var instance))
                    {
                        instance.Position = layout2d.Position;
                        instance.RotationEuler = layout2d.RotationEuler;
                        instance.Scale = layout2d.Scale;
                    }
                }
                else if (TryGetBuddyTransform3DLayout(msg, out var layout3d))
                {
                    _buddyLayoutRepository.Get(msg.BuddyId).AddOrUpdate(msg.Name, layout3d);
                    if (_transformInstanceRepository.TryGetTransform3D(msg.BuddyId, msg.Name, out var instance))
                    {
                        ApplyLayout3D(instance, layout3d);
                    }
                }
            }
            catch (Exception ex)
            {
                // NOTE: コーディングエラーでのみ到達し、ユーザー起因では到達しない想定
                LogOutput.Instance.Write(ex);
            }
        }

        private void RefreshBuddyLayout(string json)
        {
            try
            {
                var settings = JsonUtility.FromJson<BuddySettingsMessage>(json);
                // リフレッシュなので、現存するプロパティをクリアして受信値で上書きする。
                // リフレッシュはBuddyが非アクティブの状態でしか発生しないはずのため、インスタンスは見に行かない 
                var layouts = _buddyLayoutRepository.Get(settings.BuddyId);
                layouts.Clear();
                foreach (var msg in settings.Properties)
                {
                    if (TryGetBuddyTransform2DLayout(msg, out var layout2d))
                    {
                        Debug.Log($"AddOrUpdate Layout2D, {json}");
                        layouts.AddOrUpdate(msg.Name, layout2d);
                    }
                    else if (TryGetBuddyTransform3DLayout(msg, out var layout3d))
                    {
                        Debug.Log($"AddOrUpdate Layout3D, {json}");
                        layouts.AddOrUpdate(msg.Name, layout3d);
                    }
                }
            }
            catch (Exception ex)
            {
                // NOTE: コーディングエラーでのみ到達し、ユーザー起因では到達しない想定
                LogOutput.Instance.Write(ex);
            }
        }

        private void CreateTransformInstance(IScriptCaller scriptCaller)
        {
            var layouts = _buddyLayoutRepository.Get(scriptCaller.BuddyId);

            var transform2DInstances = new Dictionary<string, BuddyTransform2DInstance>();
            foreach (var pair in layouts.Transform2Ds)
            {
                var instance = _spriteCanvas.CreateTransform2DInstance();
                instance.BuddyId = scriptCaller.BuddyId;
                instance.InstanceName = pair.Key;

                instance.Position = pair.Value.Position;
                instance.RotationEuler = pair.Value.RotationEuler;
                instance.Scale = pair.Value.Scale;
                transform2DInstances[pair.Key] = instance;
                _transformInstanceRepository.AddTransform2D(scriptCaller.BuddyId, pair.Key, instance);
            }

            var transform3DInstances = new Dictionary<string, BuddyTransform3DInstance>();
            foreach (var pair in layouts.Transform3Ds)
            {
                var instance = _buddy3DInstanceCreator.CreateTransform3D();
                instance.BuddyId = scriptCaller.BuddyId;
                instance.InstanceName = pair.Key;

                ApplyLayout3D(instance, pair.Value);
                
                transform3DInstances[pair.Key] = instance;
                _transformInstanceRepository.AddTransform3D(scriptCaller.BuddyId, pair.Key, instance);
            }

            scriptCaller.SetTransformsApi(new TransformsApi(
                transform2DInstances,
                transform3DInstances
            ));
        }

        private void DeleteTransformInstance(IScriptCaller scriptCaller) 
            => _transformInstanceRepository.DeleteInstance(scriptCaller.BuddyId);

        private void ApplyLayout3D(BuddyTransform3DInstance instance, BuddyTransform3DLayout layout3d)
        {
            instance.LocalPosition = layout3d.Position;
            instance.LocalRotation = layout3d.Rotation;
            instance.Scale = layout3d.Scale;

            var parentChanged = (
                layout3d.HasParentBone != instance.HasParentBone ||
                layout3d.ParentBone != instance.ParentBone
            );
            instance.HasParentBone = layout3d.HasParentBone;
            instance.ParentBone = layout3d.ParentBone;
            if (parentChanged && _hasModel)
            {
                if (layout3d.HasParentBone)
                {
                    instance.SetParent(_animator.GetBoneTransformAscending(layout3d.ParentBone));
                }
                else
                {
                    instance.RemoveParent();
                }
            }
        }
        
        private bool TryGetBuddyTransform2DLayout(BuddySettingsPropertyMessage msg, out BuddyTransform2DLayout result)
        {
            if (msg.Type != nameof(BuddyPropertyType.Transform2D))
            {
                result = default;
                return false;
            }

            var v = msg.Transform2DValue;
            result = new BuddyTransform2DLayout(
                v.Position.ToVector2(),
                v.Rotation.ToVector3(),
                v.Scale
            );
            return true;
        }
        
        private bool TryGetBuddyTransform3DLayout(BuddySettingsPropertyMessage msg, out BuddyTransform3DLayout result)
        {
            if (msg.Type != nameof(BuddyPropertyType.Transform3D))
            {
                result = default;
                return false;
            }

            var v = msg.Transform3DValue;
            HumanBodyBones? parentBone = null;
            var rawParentBone = v.ParentBone;
            if (rawParentBone > 0 && rawParentBone < (int)HumanBodyBones.LastBone)
            {
                parentBone = (HumanBodyBones)rawParentBone;
            }
            result = new BuddyTransform3DLayout(
                v.Position.ToVector3(),
                v.Rotation.ToQuaternion(),
                v.Scale,
                parentBone
            );
            return true;
        }

        private void ResetTransformControlMode()
        {
            foreach (var transform in _transformInstanceRepository.GetTransform3DInstances())
            {
                transform.SetTransformControlActive(false);
            }
        }

        private void ControlTransform3Ds(TransformControlRequest request)
        {
            foreach (var transform in _transformInstanceRepository.GetTransform3DInstances())
            {
                transform.SetTransformControlActive(true);
                transform.SetTransformControlRequest(request);
            }
        }
    }
}
