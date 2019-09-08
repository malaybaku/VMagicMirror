using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UniRx;
using UniHumanoid;
using VRM;

namespace Baku.VMagicMirror
{
    using static ExceptionUtils;

    public class VRMLoadController : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        //TODO: この辺はちょっと分けたい気がしないでもない
        [SerializeField]
        private VrmLoadSetting loadSetting;

        [SerializeField]
        private AnimMorphEasedTarget animMorphEasedTarget = null;

        [SerializeField]
        private FaceBlendShapeController faceBlendShapeController = null;

        [SerializeField]
        private FaceAttitudeController faceAttitudeController = null;

        [SerializeField]
        private FaceDetector faceDetector = null;

        [SerializeField]
        private BlendShapeAssignController blendShapeAssignController = null;

        [SerializeField]
        private WindowStyleController windowStyleController = null;

        [SerializeField]
        private SettingAutoAdjuster settingAdjuster = null;

        [SerializeField]
        private VRMPreviewCanvas previewCanvas = null;

        [SerializeField]
        private WordToMotionController wordToMotion = null;

        [SerializeField]
        private RuntimeAnimatorController animatorController = null;

        [Serializable]
        public class VrmLoadedEvent : UnityEvent<VrmLoadedInfo>{}
        
        [SerializeField] private VrmLoadedEvent vrmLoaded = new VrmLoadedEvent();

        [SerializeField] private UnityEvent vrmDisposing = new UnityEvent();

        private HumanPoseTransfer _humanPoseTransferTarget = null;

        private void Start()
        {
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
                    case MessageCommandNames.AccessToVRoidHub:
                        //何もしない: ちゃんとUI整うまでは完全非サポート化する
                        break;
                    default:
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
                        previewCanvas.Show(context);
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
                //context.ShowMeshes();
                context.EnableUpdateWhenOffscreen();
                context.ShowMeshes();
                SetModel(context.Root);
            });
        }

        private void OnVrmLoadedFromVRoidHub(string modelId, GameObject vrmObject)
        {
            //TODO: Debug
            SetModel(vrmObject);
        }

        private void ReleaseCurrentVrm()
        {
            // cleanup
            var loaded = _humanPoseTransferTarget;
            _humanPoseTransferTarget = null;

            if (loaded != null)
            {
                vrmDisposing.Invoke();

                //TODO: イベントハンドラ頼みになるよう直す
                animMorphEasedTarget.blendShapeProxy = null;
                faceBlendShapeController?.DisposeProxy();
                faceAttitudeController?.DisposeHead();
                faceDetector.DisposeNonCameraBlinkComponent();
                blendShapeAssignController.DisposeModel();
                windowStyleController.DisposeModelRenderers();
                settingAdjuster.DisposeModelRoot();
                wordToMotion.Dispose();
                loadSetting.ikWeightCrossFade.DisposeIk();

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
            if (lookAt != null)
            {
                _humanPoseTransferTarget = go.AddComponent<HumanPoseTransfer>();
                _humanPoseTransferTarget.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.None;
            }

            //NOTE: ここからは一部がコケても他のセットアップを続けて欲しいので、やや細かくTry Catchしていく
            //TODO: スケールしなくなってるのでそろそろLoaded的なイベント化したい

            TryWithoutException(() =>
                VRMLoadControllerHelper.SetupVrm(go, loadSetting, faceDetector)
                );
            //セットアップの過程でFinalIKに触るため、(有償アセットなので取り外しの事も考えつつ)ファイル分離

            var animator = go.GetComponent<Animator>();
            var blendShapeProxy = go.GetComponent<VRMBlendShapeProxy>();

            TryWithoutException(() =>
            {
                animMorphEasedTarget.blendShapeProxy = blendShapeProxy;
                faceBlendShapeController?.Initialize(blendShapeProxy);
                faceAttitudeController?.Initialize(
                    animator.GetBoneTransform(HumanBodyBones.Neck),
                    animator.GetBoneTransform(HumanBodyBones.Head)
                    );
            });

            TryWithoutException(() =>
            {
                blendShapeAssignController.InitializeModel(go.transform);
                var renderers = go.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    //セルフシャドウは明示的に切る: ちょっとでも軽量化したい
                    r.receiveShadows = false;
                }
                windowStyleController.InitializeModelRenderers(renderers);

                settingAdjuster.AssignModelRoot(go.transform);
                blendShapeAssignController.SendBlendShapeNames();

                var simpleAnimation = go.AddComponent<SimpleAnimation>();
                simpleAnimation.playAutomatically = false;
                wordToMotion.Initialize(simpleAnimation, blendShapeProxy, _humanPoseTransferTarget, animator);

                animator.runtimeAnimatorController = animatorController;
            });
            
            vrmLoaded.Invoke(new VrmLoadedInfo()
            {
                vrmRoot = go.transform,
                animator = animator,
                blendShape = blendShapeProxy,
            });
        }
        
        [Serializable]
        public struct VrmLoadSetting
        {
            public Transform bodyEndTarget;
            public Transform bodyRootTarget;
            public Transform leftHandTarget;
            public Transform rightHandTarget;
            public Transform rightIndexTarget;
            public Transform headTarget;
            //public InputDeviceToMotion inputToMotion;
            public IkWeightCrossFade ikWeightCrossFade;
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
