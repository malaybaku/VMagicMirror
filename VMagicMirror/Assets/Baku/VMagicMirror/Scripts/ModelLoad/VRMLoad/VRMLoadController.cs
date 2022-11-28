using System;
using System.IO;
using System.Threading;
using Baku.VMagicMirror.IK;
using Cysharp.Threading.Tasks;
using UniGLTF;
using UnityEngine;
using UniHumanoid;
using UniRx;
using VRM;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>VRMのロード処理をやるやつ</summary>
    public class VRMLoadController : MonoBehaviour, IVRMLoadable
    {
        //[SerializeField] private VrmLoadSetting loadSetting = default;
        [SerializeField] private RuntimeAnimatorController animatorController = null;

        public event Action<VrmLoadedInfo> PreVrmLoaded;
        public event Action<VrmLoadedInfo> VrmLoaded; 
        public event Action<VrmLoadedInfo> PostVrmLoaded; 
        public event Action VrmDisposing;

        private IMessageSender _sender;
        private IKTargetTransforms _ikTargets = null;
        private VRMPreviewCanvas _previewCanvas = null;
        private HumanPoseTransfer _humanPoseTransferTarget = null;
        private ErrorIndicateSender _errorSender = null;
        private ErrorInfoFactory _errorInfoFactory = null;

        //NOTE: 使わないはずだが一応こう書いておく
        public IReadOnlyReactiveProperty<CurrentModelVersion> ModelVersion { get; }
            = new ReactiveProperty<CurrentModelVersion>(CurrentModelVersion.Vrm0x);

        [Inject]
        public void Initialize(
            IMessageSender sender,
            IMessageReceiver receiver,
            VRMPreviewCanvas previewCanvas,
            IKTargetTransforms ikTargets,
            ErrorIndicateSender errorSender,
            ErrorInfoFactory errorInfoFactory
            )
        {
            _sender = sender;
            _previewCanvas = previewCanvas;
            _ikTargets = ikTargets;

            _errorSender = errorSender;
            _errorInfoFactory = errorInfoFactory;
            
            receiver.AssignCommandHandler(
                VmmCommands.OpenVrmPreview,
                message => LoadModelForPreview(message.Content)
                );
            receiver.AssignCommandHandler(
                VmmCommands.OpenVrm,
                message =>
                {
                    previewCanvas.Hide();
                    LoadModel(message.Content);
                });
            receiver.AssignCommandHandler(
                VmmCommands.CancelLoadVrm,
                _ => previewCanvas.Hide()
                );
        }

        private void LoadModelForPreview(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            byte[] bytes = Array.Empty<byte>();
            try
            {
                if (Path.GetExtension(path).ToLower() == ".vrm")
                {
                    bytes = File.ReadAllBytes(path);
                    var parser = new GlbLowLevelParser("", bytes);
                    using var data = parser.Parse();
                    using var context = new VRMImporterContext(new VRMData(data));
                    //すぐなくなるので削除します、using statementのトレーサビリティ都合で…
                    //_previewCanvas.Show(context);
                }
                else
                {
                    LogOutput.Instance.Write("unknown file type: " + path);
                }
            }
            catch (Exception ex)
            {
                CheckVrm10AndHandleError(ex, bytes, path, this.GetCancellationTokenOnDestroy()).Forget();
            }
        }

        private void LoadModel(string path)
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

            byte[] bytes = Array.Empty<byte>();
            try
            {
                bytes = File.ReadAllBytes(path);
                var parser = new GlbLowLevelParser("", bytes);
                using var data = parser.Parse();
                using var context = new VRMImporterContext(new VRMData(data));
                var meta = context.ReadMeta(false);
                
                var instance = context.Load();
                instance.EnableUpdateWhenOffscreen();
                instance.ShowMeshes();
                _sender.SendCommand(MessageFactory.Instance.ModelNameConfirmedOnLoad("VRM File: " + meta.Title));
                SetModel(instance.Root);
            }
            catch (Exception ex)
            {
                CheckVrm10AndHandleError(ex, bytes, path, this.GetCancellationTokenOnDestroy()).Forget();
            }
        }

        public void OnVrmLoadedFromVRoidHub(string modelId, GameObject vrmObject)
        {
            try 
            {
                //オクルージョン関連は切っておく:
                //context.EnableUpdateWhenOffscreen()に相当する処理
                foreach(var sr in vrmObject.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    sr.updateWhenOffscreen = true;
                }
                SetModel(vrmObject);
            }
            catch (Exception ex)
            {    
                //NOTE: ここでVRM1.0が来たかどうか知れると良いが、GameObjectで投げ込まれると対処できないので諦める
                //(そもそもVRoidのUIでメタデータ表示に失敗しそうな気もするが)
                HandleLoadError(ex);
            }
        }

        //モデルの破棄
        private void ReleaseCurrentVrm()
        {
            var loaded = _humanPoseTransferTarget;
            _humanPoseTransferTarget = null;

            if (loaded != null)
            {
                VrmDisposing?.Invoke();
                Destroy(loaded.gameObject);
            }
        }

        private void SetModel(GameObject go)
        {
            ReleaseCurrentVrm();

            if (go == null)
            {
                return;
            }

            _humanPoseTransferTarget = go.AddComponent<HumanPoseTransfer>();
            _humanPoseTransferTarget.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.None;

            //セットアップのうちFinalIKに思い切り依存した所が別スクリプトになってます
            VRMLoadControllerHelper.SetupVrm(go, _ikTargets);

            var animator = go.GetComponent<Animator>();
            animator.runtimeAnimatorController = animatorController;
            
            var blendShapeProxy = go.GetComponent<VRMBlendShapeProxy>();
            
            var renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                //セルフシャドウは明示的に切る: ちょっとでも軽量化したい
                r.receiveShadows = false;
            }
            
            var info = new VrmLoadedInfo()
            {
                vrmRoot = go.transform,
                //animator = animator,
                //クラスごと削除予定なので、この粒度のコンパイラ警告はむしろ邪魔
                //blendShape = blendShapeProxy,
                renderers = renderers,
            };
            
            PreVrmLoaded?.Invoke(info);
            VrmLoaded?.Invoke(info);
            PostVrmLoaded?.Invoke(info);
        }

        private async UniTaskVoid CheckVrm10AndHandleError(
            Exception exceptionOnLoadPreview,
            byte[] binary, 
            string pathOrModelName,
            CancellationToken cancellationToken
        )
        {
            if (File.Exists(pathOrModelName))
            {
                //フルパスだとUI表示には長いので絞ってしまう
                pathOrModelName = Path.GetFileName(pathOrModelName);
            }
            
            try
            {
                var isVrm10 = await Vrm10Validator.CheckModelIsVrm10(binary, cancellationToken);
                if (isVrm10)
                {
                    _sender.SendCommand(
                        MessageFactory.Instance.VRM10SpecifiedButNotSupported(pathOrModelName)
                        );
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                //ignore
            }
            catch (Exception)
            {
                //2回目のロード時に起きたエラーは無視し、最初のロード失敗で出たエラー情報を送る
            }

            HandleLoadError(exceptionOnLoadPreview);
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
