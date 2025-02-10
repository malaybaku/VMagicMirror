using System;
using System.Collections.Generic;
using Baku.VMagicMirror.Buddy;
using Baku.VMagicMirror.LuaScript.Api;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.LuaScript
{
    /// <summary>
    /// WPFからBuddyのプロパティ情報を受けてレポジトリに保存するクラス
    /// </summary>
    public class BuddyLayoutUpdater : PresenterBase
    {
        private readonly IMessageReceiver _receiver;
        private readonly IVRMLoadable _vrmLoadable;
        private readonly ScriptLoader _scriptLoader;
        private readonly LuaScriptSpriteCanvas _spriteCanvas;
        private readonly BuddyLayoutRepository _layoutLayoutRepository;
        private readonly BuddyTransformInstanceRepository _transformInstanceRepository;
        // NOTE: インスタンスのキャッシュは誰が持つ？
        //このクラスで持つならクラス名がUpdaterじゃなさそうだが

        public BuddyLayoutUpdater(
            IMessageReceiver receiver, 
            IVRMLoadable vrmLoadable,
            ScriptLoader scriptLoader,
            LuaScriptSpriteCanvas spriteCanvas,
            BuddyLayoutRepository layoutRepository,
            BuddyTransformInstanceRepository transformInstanceRepository)
        {
            _receiver = receiver;
            _vrmLoadable = vrmLoadable;
            _scriptLoader = scriptLoader;
            _spriteCanvas = spriteCanvas;
            _layoutLayoutRepository = layoutRepository;
            _transformInstanceRepository = transformInstanceRepository;
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

            // NOTE: スクリプトのロード時点でLayout情報は既に手元にあるのが前提になっている
            _scriptLoader.ScriptLoading
                .Subscribe(CreateTransformInstance)
                .AddTo(this);
            _scriptLoader.ScriptDisposing
                .Subscribe(DeleteTransformInstance)
                .AddTo(this);
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            // ロード済みのインスタンスで、VRMのボーンにアタッチしたいようなものがあれば実際にアタッチする
            // TODO: 3Dの実装が進行したら中身を入れる。Canvasベースの2Dでは何もしないでOK
        }

        private void OnVrmDisposing()
        {
            // VRMのボーンにアタッチ済みだったTransformがあったらそれを剥がす(剥がさないと一緒に破棄されちゃうので)
            // TODO: 3Dの実装が進行したら中身を入れる。Canvasベースの2Dでは何もしないでOK
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
                    _layoutLayoutRepository.Get(msg.BuddyId).AddOrUpdate(msg.Name, layout2d);
                    if (_transformInstanceRepository.TryGetTransform2D(msg.BuddyId, msg.Name, out var instance))
                    {
                        instance.Position = layout2d.Position;
                        instance.RotationEuler = layout2d.RotationEuler;
                        instance.Scale = layout2d.Scale;
                    }
                }
                else if (TryGetBuddyTransform3DLayout(msg, out var layout3d))
                {
                    _layoutLayoutRepository.Get(msg.BuddyId).AddOrUpdate(msg.Name, layout3d);
                    //TODO: ここでもインスタンスへの値の適用がしたい
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
                var layouts = _layoutLayoutRepository.Get(settings.BuddyId);
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

        private void CreateTransformInstance(ScriptCaller scriptCaller)
        {
            var layouts = _layoutLayoutRepository.Get(scriptCaller.BuddyId);

            var transform2DInstances = new Dictionary<string, LuaScriptTransform2DInstance>();
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

            //TODO: 3Dも同じような流れで追加する

            scriptCaller.SetTransformsApi(new TransformsApi(
                transform2DInstances
            ));
        }

        private void DeleteTransformInstance(ScriptCaller scriptCaller) 
            => _transformInstanceRepository.DeleteInstance(scriptCaller.BuddyId);

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
    }
}
