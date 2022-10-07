using System;
using System.IO;
using System.Threading;
using Baku.VMagicMirror.IK;
using Cysharp.Threading.Tasks;
using RootMotion.FinalIK;
using UniGLTF.Extensions.VRMC_vrm;
using UniRx;
using UnityEngine;
using UniVRM10;
using UniVRM10.Migration;
using Zenject;
using Object = UnityEngine.Object;

namespace Baku.VMagicMirror
{
    public enum CurrentModelVersion
    {
        Unloaded,
        Vrm0x,
        Vrm10,
    }

    /// <summary>VRMのロード処理をやるやつで、VRM 0.xとVRM 1.0両方に対応するようなもの</summary>
    public class VRM10LoadController : IVRMLoadable, IInitializable, IDisposable
    {
        public event Action<VrmLoadedInfo> PreVrmLoaded;
        public event Action<VrmLoadedInfo> VrmLoaded; 
        public event Action<VrmLoadedInfo> PostVrmLoaded; 
        public event Action VrmDisposing;

        private readonly IMessageSender _sender;
        private readonly IMessageReceiver _receiver;
        private readonly BuiltInMotionClipData _builtInClip;
        private readonly IKTargetTransforms _ikTargets;
        private readonly VrmLoadProcessBroker _previewBroker;
        private readonly ErrorIndicateSender _errorSender;
        private readonly ErrorInfoFactory _errorInfoFactory;

        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private Vrm10Instance _instance = null;
        //NOTE: controlRigはinstanceとライフサイクルが違う(可能性がある)ので要注意
        private Vrm10RuntimeControlRig _controlRig = null;
        
        private readonly ReactiveProperty<CurrentModelVersion> _modelVersion =
            new ReactiveProperty<CurrentModelVersion>(CurrentModelVersion.Unloaded);
        public IReadOnlyReactiveProperty<CurrentModelVersion> ModelVersion => _modelVersion;
        
        [Inject]
        public VRM10LoadController(
            IMessageSender sender,
            IMessageReceiver receiver,
            BuiltInMotionClipData builtInClip,
            VrmLoadProcessBroker previewBroker,
            IKTargetTransforms ikTargets,
            ErrorIndicateSender errorSender,
            ErrorInfoFactory errorInfoFactory
            )
        {
            _sender = sender;
            _receiver = receiver;
            _builtInClip = builtInClip;
            _previewBroker = previewBroker;
            _ikTargets = ikTargets;
            _errorSender = errorSender;
            _errorInfoFactory = errorInfoFactory;
        }

        public void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.OpenVrmPreview,
                message => LoadModelForPreview(message.Content).Forget()
            );
            _receiver.AssignCommandHandler(
                VmmCommands.OpenVrm,
                message =>
                {
                    _previewBroker.RequestHide();
                    LoadModel(message.Content).Forget();
                });
            _receiver.AssignCommandHandler(
                VmmCommands.CancelLoadVrm,
                _ => _previewBroker.RequestHide()
            );

            _previewBroker.VRoidModelLoaded
                .Subscribe(v =>
                {
                    OnVrmLoadedFromVRoidHub(v.modelId, v.instance, v.isVrm10);
                })
                .AddTo(_disposable);
        }

        public void Dispose()
        {
            _disposable.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();
        }
        
        private async UniTaskVoid LoadModelForPreview(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            if (Path.GetExtension(path).ToLower() != ".vrm")
            {
                LogOutput.Instance.Write($"unknown file type: {path}");
                return;
            }

            Vrm10Instance instance = null;
            try
            {
                var bytes = File.ReadAllBytes(path);
                //コールバックが呼ばれて終わり、インスタンスはすぐ捨てる
                instance = await Vrm10.LoadBytesAsync(bytes,
                    true,
                    ControlRigGenerationOption.None,
                    false,
                    vrmMetaInformationCallback: OnMetaDetectedForPreview,
                    ct: _cts.Token
                );
            }
            catch (Exception ex)
            {
                HandleLoadError(ex);
            }
            finally
            {
                if (instance != null)
                {
                    Object.Destroy(instance.gameObject);
                }
            }
        }

        private async UniTaskVoid LoadModel(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            if (Path.GetExtension(path).ToLower() != ".vrm")
            {
                LogOutput.Instance.Write($"unknown file type: {path}");
                return;
            }

            try
            {
                var bytes = File.ReadAllBytes(path);
                var instance = await Vrm10.LoadBytesAsync(bytes,
                    true,
                    ControlRigGenerationOption.Generate,
                    true,
                    vrmMetaInformationCallback: OnMetaDetectedForModelLoad,
                    ct: _cts.Token
                );

                //NOTE: VRM 0.xのcontext.EnableUpdateWhenOffscreen();に相当する。
                //LoadBytesAsyncの一貫で実行されてそうだったら消してもいい
                foreach(var sr in instance.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    sr.updateWhenOffscreen = true;
                }
                
                _sender.SendCommand(
                    MessageFactory.Instance.ModelNameConfirmedOnLoad("VRM File: " + instance.Vrm.Meta.Name)
                    );
                SetModel(instance);
            }
            catch (Exception ex)
            {
                HandleLoadError(ex);
            }
        }

        private void OnMetaDetectedForPreview(Texture2D thumbnail, Meta vrm10meta, Vrm0Meta vrm0meta)
        {
            if (vrm10meta == null && vrm0meta == null)
            {
                LogOutput.Instance.Write("Error: Both VRM 0.x / VRM 1.0 meta were not found");
                return;
            }
            
            if (vrm10meta != null)
            {
                _previewBroker.RequestShowVrm1Meta(vrm10meta, thumbnail);
            }
            else
            {
                _previewBroker.RequestShowVrm0Meta(vrm0meta, thumbnail);
            }
        }

        private void OnMetaDetectedForModelLoad(Texture2D thumbnail, Meta vrm10meta, Vrm0Meta vrm0meta)
        {
            _modelVersion.Value = vrm10meta != null ? CurrentModelVersion.Vrm10 : CurrentModelVersion.Vrm0x;
        }

        public void OnVrmLoadedFromVRoidHub(string modelId, Vrm10Instance instance, bool isVrm10)
        {
            try 
            {
                //オクルージョン関連は切っておく:
                //context.EnableUpdateWhenOffscreen()に相当する処理
                foreach(var sr in instance.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    sr.updateWhenOffscreen = true;
                }
                _modelVersion.Value = isVrm10 ? CurrentModelVersion.Vrm10 : CurrentModelVersion.Vrm0x;
                SetModel(instance);
            }
            catch (Exception ex)
            {    
                //NOTE: ここでVRM1.0が来たかどうか知れると良いが、GameObjectで投げ込まれると対処できないので諦める
                //(そもそもVRoidのUIでメタデータ表示に失敗しそうな気もするが)
                HandleLoadError(ex);
            }
        }

        private void ReleaseCurrentVrm()
        {
            //モデルがロード前のときはイベント発火しないようにする。
            //これは理想挙動のためではなく、従来実装に合わすことでバグを起きにくくするため
            if (_instance != null)
            {
                VrmDisposing?.Invoke();
            }

            //NOTE: 分けたほうが良さそうに見えてるので分けてるが、不要なら_instanceのDestroyだけで済ます
            // _controlRig?.Dispose();
            // _controlRig = null;

            if (_instance != null)
            {
                Object.Destroy(_instance.gameObject);
            }
            _instance = null;
            _modelVersion.Value = CurrentModelVersion.Unloaded;
        }

        private void SetModel(Vrm10Instance instance)
        {
            ReleaseCurrentVrm();

            if (instance == null)
            {
                return;
            }

            _instance = instance;
            //NOTE: Script Execution OrderをVMM側で制御したいので。
            instance.UpdateType = Vrm10Instance.UpdateTypes.None;
            var go = instance.gameObject;

            //セットアップのうちFinalIKに思い切り依存した所が別スクリプトになってます
            VRM10LoadControllerHelper.SetupVrm(instance, _ikTargets);

            var animator = go.GetComponent<Animator>();
            animator.runtimeAnimatorController = _builtInClip.DefaultAnimatorController;
            
            var renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                //セルフシャドウは明示的に切る: ちょっとでも軽量化したい
                r.receiveShadows = false;
            }
            
            var info = new VrmLoadedInfo()
            {
                modelVersion = _modelVersion.Value,
                vrmRoot = go.transform,
                controlRig = instance.Runtime.ControlRig,
                animator = animator,
                instance = instance,
                fbbIk = go.GetComponentInChildren<FullBodyBipedIK>(),
                //NOTE: このbsがないことでエラーが起こるのはイベント購読側が悪い。
                //blendShape = blendShapeProxy,
                renderers = renderers,
            };
            
            PreVrmLoaded?.Invoke(info);
            VrmLoaded?.Invoke(info);
            PostVrmLoaded?.Invoke(info);
        }

        private void HandleLoadError(Exception ex)
        {
            string logContent =
                _errorInfoFactory.LoadVrmErrorContentPrefix() +
                "--\n" + LogOutput.ExToString(ex) + "\n--";

            LogOutput.Instance.Write(logContent);
            _errorSender.SendError(
                _errorInfoFactory.LoadVrmErrorTitle(),
                logContent,
                ErrorIndicateSender.ErrorLevel.Error
                );
        }

        [Serializable]
        public struct VrmLoadSetting
        {
            public Transform bodyTarget;
            public Transform leftHandTarget;
            public Transform rightHandTarget;
            public Transform rightIndexTarget;
            public Transform headTarget;
        }
    }
}
