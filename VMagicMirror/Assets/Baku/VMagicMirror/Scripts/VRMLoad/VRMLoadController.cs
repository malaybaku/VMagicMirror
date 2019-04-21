using System;
using System.IO;
using UnityEngine;
using UniRx;
using UniHumanoid;
using VRM;

namespace Baku.VMagicMirror
{
    public class VRMLoadController : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        [SerializeField]
        private VRMInformation vrmInformation = null;

        //TODO: この辺はちょっと分けたい気がしないでもない
        [SerializeField]
        private VrmLoadSetting loadSetting;

        [SerializeField]
        private AnimMorphEasedTarget animMorphEasedTarget = null;

        [SerializeField]
        private VRoidSDK.Example.VRoidHubController vroidHub = null;

        private HumanPoseTransfer m_loaded = null;

        private void Start()
        {
            vroidHub?.SetOnLoadHandler(OnVrmLoadedFromVRoidHub);

            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.OpenVrmPreview:
                        LoadModelForPreview(message.Content);
                        break;
                    case MessageCommandNames.OpenVrm:
                        vrmInformation.Hide();
                        LoadModel(message.Content);
                        break;
                    case MessageCommandNames.CancelLoadVrm:
                        vrmInformation.Hide();
                        break;
                    case MessageCommandNames.AccessToVRoidHub:
                        vroidHub?.Open();
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

            if (Path.GetExtension(path).ToLower() == ".vrm")
            {
                using (var context = new VRMImporterContext())
                {
                    context.ParseGlb(File.ReadAllBytes(path));
                    vrmInformation.ShowMetaData(context);
                }
            }
            else
            {
                Debug.LogWarningFormat("unknown file type: {0}", path);
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
                Debug.LogWarning($"unknown file type: {path}");
                return;
            }

            var context = new VRMImporterContext();
            var file = File.ReadAllBytes(path);
            context.ParseGlb(file);

            context.Load();
            context.ShowMeshes();
            context.EnableUpdateWhenOffscreen();
            context.ShowMeshes();
            SetModel(context.Root);
        }

        private void OnVrmLoadedFromVRoidHub(string modelId, GameObject vrmObject)
        {
            //TODO: Debug
            SetModel(vrmObject);
        }

        private void ReleaseCurrentVrm()
        {
            // cleanup
            var loaded = m_loaded;
            m_loaded = null;

            if (loaded != null)
            {
                //破棄済みオブジェクトに触らせないためにnullize
                loadSetting.inputToMotion.fingerAnimator = null;
                loadSetting.inputToMotion.vrmRoot = null;
                loadSetting.inputToMotion.rightShoulder = null;
                animMorphEasedTarget.blendShapeProxy = null;

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
                m_loaded = go.AddComponent<HumanPoseTransfer>();
                m_loaded.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.None;
            }

            //セットアップの過程でFinalIKに触るため、(有償アセットなので取り外しの事も考えつつ)ファイル分離
            VRMLoadControllerHelper.SetupVrm(go, loadSetting);

            loadSetting.inputToMotion.fingerAnimator = go.GetComponent<FingerAnimator>();
            loadSetting.inputToMotion.vrmRoot = go.transform;
            loadSetting.inputToMotion.rightShoulder = go.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightShoulder);
            animMorphEasedTarget.blendShapeProxy = go.GetComponent<VRMBlendShapeProxy>();
        }

        [Serializable]
        public struct VrmLoadSetting
        {
            public Transform bodyTarget;
            public Transform leftHandTarget;
            public Transform rightHandTarget;
            public Transform headTarget;
            public InputDeviceToMotion inputToMotion;
            public Transform presentationTarget;
        }

    }
}
