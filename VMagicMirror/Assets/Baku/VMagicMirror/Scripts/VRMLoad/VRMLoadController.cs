using System;
using System.IO;
using Baku.VMagicMirror.IK;
using UnityEngine;
using UniRx;
using UniHumanoid;
using VRM;
using Zenject;

namespace Baku.VMagicMirror
{
    using static ExceptionUtils;

    /// <summary>VRMのロード処理をやるやつ</summary>
    public class VRMLoadController : MonoBehaviour, IVRMLoadable
    {
        //[SerializeField] private VrmLoadSetting loadSetting = default;
        [SerializeField] private RuntimeAnimatorController animatorController = null;

        //NOTE: 下の2つは参照外せなくもないが残している。他と同じでイベントで初期化/破棄するスタイルでもよい
        // - WindowStyleControllerは受け取るデータがちょっと特別なため
        // - SettingAutoAdjusterはロード直後に特殊処理が入る可能性があるため
        [SerializeField] private WindowStyleController windowStyleController = null;
        [SerializeField] private SettingAutoAdjuster settingAdjuster = null;

        public event Action<VrmLoadedInfo> PreVrmLoaded;
        public event Action<VrmLoadedInfo> VrmLoaded; 
        public event Action VrmDisposing;

        private IKTargetTransforms _ikTargets = null;
        private VRMPreviewCanvas _previewCanvas = null;
        private HumanPoseTransfer _humanPoseTransferTarget = null;

        [Inject]
        public void Initialize(
            ReceivedMessageHandler handler,
            VRMPreviewCanvas previewCanvas,
            IKTargetTransforms ikTargets
            )
        {
            _previewCanvas = previewCanvas;
            _ikTargets = ikTargets;
            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.OpenVrmPreview:
                        LoadModelForPreview(message.Content);
                        break;
                    case MessageCommandNames.OpenVrm:
                        previewCanvas.Hide();
                        LoadModel(message.Content);
                        break;
                    case MessageCommandNames.CancelLoadVrm:
                        previewCanvas.Hide();
                        break;
                }
            });
        }

        private void LoadModelForPreview(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            TryWithoutException(() =>
            {
                if (Path.GetExtension(path).ToLower() == ".vrm")
                {
                    using (var context = new VRMImporterContext())
                    {
                        context.ParseGlb(File.ReadAllBytes(path));
                        _previewCanvas.Show(context);
                    }
                }
                else
                {
                    LogOutput.Instance.Write("unknown file type: " + path);
                }
            });
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

            TryWithoutException(() =>
            {
                var context = new VRMImporterContext();
                var file = File.ReadAllBytes(path);
                context.ParseGlb(file);

                context.Load();
                context.EnableUpdateWhenOffscreen();
                context.ShowMeshes();
                SetModel(context.Root);
            });
        }

        public void OnVrmLoadedFromVRoidHub(string modelId, GameObject vrmObject)
        {
            SetModel(vrmObject);
        }

        //モデルの破棄
        private void ReleaseCurrentVrm()
        {
            var loaded = _humanPoseTransferTarget;
            _humanPoseTransferTarget = null;

            if (loaded != null)
            {
                VrmDisposing?.Invoke();

                windowStyleController.DisposeModelRenderers();
                settingAdjuster.DisposeModelRoot();
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

            var lookAt = go.GetComponent<VRMLookAtHead>();
            _humanPoseTransferTarget = go.AddComponent<HumanPoseTransfer>();
            _humanPoseTransferTarget.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.None;
            lookAt.UpdateType = UpdateType.LateUpdate;
            
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
            windowStyleController.InitializeModelRenderers(renderers);
            settingAdjuster.AssignModelRoot(go.transform);

            var info = new VrmLoadedInfo()
            {
                vrmRoot = go.transform,
                animator = animator,
                blendShape = blendShapeProxy,
            };
            
            PreVrmLoaded?.Invoke(info);
            VrmLoaded?.Invoke(info);
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
    
    [Serializable]
    public struct VrmLoadedInfo
    {
        public Transform vrmRoot;
        public Animator animator;
        public VRMBlendShapeProxy blendShape;
    }
}
