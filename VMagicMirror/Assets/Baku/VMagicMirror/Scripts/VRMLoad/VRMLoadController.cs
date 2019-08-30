using System;
using System.IO;
using UnityEngine;
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
                //TODO: スケールしなくなってるのでそろそろReleaseイベント化したい
                //破棄済みオブジェクトに触らせないためにnullize
                loadSetting.inputToMotion.fingerAnimator = null;
                loadSetting.inputToMotion.vrmRoot = null;
                loadSetting.inputToMotion.head = null;
                loadSetting.inputToMotion.rightShoulder = null;
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

            TryWithoutException(() =>
            {
                loadSetting.inputToMotion.fingerAnimator = go.GetComponent<FingerAnimator>();
                loadSetting.inputToMotion.vrmRoot = go.transform;
            });

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

                loadSetting.inputToMotion.head = animator.GetBoneTransform(HumanBodyBones.Head);
                loadSetting.inputToMotion.rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
                loadSetting.inputToMotion.fingerRig = animator
                    .GetBoneTransform(HumanBodyBones.RightHand)
                    .GetComponent<RootMotion.FinalIK.FingerRig>();
                go.GetComponent<MotionModifyToMotion>()
                    .SetReceiver(GetComponent<MotionModifyReceiver>());
                loadSetting.inputToMotion.PressKeyMotion("LControlKey");
                loadSetting.inputToMotion.PressKeyMotion("RControlKey");
            });

            TryWithoutException(() =>
            {
                blendShapeAssignController.InitializeModel(go.transform);
                var renderers = go.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    //セルフシャドウは明示的に切る: ちょっとでも軽量化したい
                    renderer.receiveShadows = false;
                }
                windowStyleController.InitializeModelRenderers(renderers);
                go.AddComponent<EyeDownOnBlink>()
                    .Initialize(
                        blendShapeProxy,
                        faceDetector,
                        wordToMotion,
                        blendShapeAssignController.EyebrowBlendShape,
                        animator.GetBoneTransform(HumanBodyBones.RightEye),
                        animator.GetBoneTransform(HumanBodyBones.LeftEye)
                        );

                settingAdjuster.AssignModelRoot(go.transform);
                blendShapeAssignController.SendBlendShapeNames();

                var simpleAnimation = go.AddComponent<SimpleAnimation>();
                simpleAnimation.playAutomatically = false;
                wordToMotion.Initialize(simpleAnimation, blendShapeProxy, _humanPoseTransferTarget, animator);

                animator.runtimeAnimatorController = animatorController;
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
            public InputDeviceToMotion inputToMotion;
            public IkWeightCrossFade ikWeightCrossFade;
        }

    }
}
