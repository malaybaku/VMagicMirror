using System;
using System.IO;
using TMPro;
using UniHumanoid;
using UnityEngine;
using UnityEngine.UI;
using VRM;

namespace Baku.VMagicMirror
{
    public class VRMLoadController : MonoBehaviour
    {
        [SerializeField]
        HumanPoseTransfer m_src;

        [SerializeField]
        GameObject Root;

        [Serializable]
        struct TextFields
        {
            [SerializeField, Header("Info")]
            TextMeshProUGUI m_textModelTitle;
            [SerializeField]
            TextMeshProUGUI m_textModelVersion;
            [SerializeField]
            TextMeshProUGUI m_textModelAuthor;
            [SerializeField]
            TextMeshProUGUI m_textModelContact;
            [SerializeField]
            TextMeshProUGUI m_textModelReference;
            [SerializeField]
            RawImage m_thumbnail;

            [SerializeField, Header("CharacterPermission")]
            TextMeshProUGUI m_textPermissionAllowed;
            [SerializeField]
            TextMeshProUGUI m_textPermissionViolent;
            [SerializeField]
            TextMeshProUGUI m_textPermissionSexual;
            [SerializeField]
            TextMeshProUGUI m_textPermissionCommercial;
            [SerializeField]
            TextMeshProUGUI m_textPermissionOther;

            [SerializeField, Header("DistributionLicense")]
            TextMeshProUGUI m_textDistributionLicense;
            [SerializeField]
            TextMeshProUGUI m_textDistributionOther;

            public void Start()
            {
                m_textModelTitle.text = "";
                m_textModelVersion.text = "";
                m_textModelAuthor.text = "";
                m_textModelContact.text = "";
                m_textModelReference.text = "";

                m_textPermissionAllowed.text = "";
                m_textPermissionViolent.text = "";
                m_textPermissionSexual.text = "";
                m_textPermissionCommercial.text = "";
                m_textPermissionOther.text = "";

                m_textDistributionLicense.text = "";
                m_textDistributionOther.text = "";
            }

            public void UpdateMeta(VRMImporterContext context)
            {
                var meta = context.ReadMeta(true);

                m_textModelTitle.text = meta.Title;
                m_textModelVersion.text = meta.Version;
                m_textModelAuthor.text = meta.Author;
                m_textModelContact.text = meta.ContactInformation;
                m_textModelReference.text = meta.Reference;

                m_textPermissionAllowed.text = meta.AllowedUser.ToString();
                m_textPermissionViolent.text = meta.ViolentUssage.ToString();
                m_textPermissionSexual.text = meta.SexualUssage.ToString();
                m_textPermissionCommercial.text = meta.CommercialUssage.ToString();
                m_textPermissionOther.text = meta.OtherPermissionUrl;

                m_textDistributionLicense.text = meta.LicenseType.ToString();
                m_textDistributionOther.text = meta.OtherLicenseUrl;

                m_thumbnail.texture = meta.Thumbnail;
            }
        }

        [SerializeField]
        TextFields m_texts;

        [SerializeField]
        InputDeviceToMotion _inputToMotion;

        //TODO: この辺はちょっと分けたい気がしないでもないが。
        [SerializeField]
        RuntimeAnimatorController runtimeController;

        [SerializeField]
        Transform bodyTarget;
        [SerializeField]
        Transform rightHandTarget;
        [SerializeField]
        Transform leftHandTarget;
        [SerializeField]
        Transform headTarget;

        [SerializeField]
        VRoidSDK.Example.VRoidHubController vroidHub = null;

        [SerializeField]
        AnimMorphEasedTarget animMorphEasedTarget;

        HumanPoseTransfer m_loaded;

        private void Start()
        {
            m_texts.Start();
            vroidHub.SetOnLoadHandler(OnVrmLoadedFromVRoidHub);
        }

        public void LoadModelOnlyForPreview(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            Debug.LogFormat("{0}", path);
            var ext = Path.GetExtension(path).ToLower();
            switch (ext)
            {
                case ".vrm":
                    using (var context = new VRMImporterContext())
                    {
                        context.ParseGlb(File.ReadAllBytes(path));
                        m_texts.UpdateMeta(context);
                    }
                    break;
                default:
                    Debug.LogWarningFormat("unknown file type: {0}", path);
                    break;
            }
        }

        public void LoadModel(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            Debug.LogFormat("{0}", path);
            string ext = Path.GetExtension(path).ToLower();
            if (ext != ".vrm")
            {
                Debug.LogWarning($"unknown file type: {path}");
                return;
            }

            var context = new VRMImporterContext();
            var file = File.ReadAllBytes(path);
            context.ParseGlb(file);
            m_texts.UpdateMeta(context);

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
                Debug.LogFormat("destroy {0}", loaded);
                //多分コレやらないとAniLipSyncが破棄済みオブジェクトを触りに行ってしまうので。
                animMorphEasedTarget.blendShapeProxy = null;
                GameObject.Destroy(loaded.gameObject);
            }
        }

        void SetModel(GameObject go)
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
                m_loaded.Source = m_src;
                m_loaded.SourceType = HumanPoseTransfer.HumanPoseTransferSourceType.HumanPoseTransfer;
            }

            var animation = go.GetComponent<Animation>();
            if (animation && animation.clip != null)
            {
                animation.Play(animation.clip.name);
            }

            //セットアップの過程でFinalIKに少し触るので(規約クリアになるよう)ファイルを分離
            VRMLoadControllerHelper.SetupVrm(go, new VRMLoadControllerHelper.VrmLoadSetting()
            {
                runtimeAnimatorController = runtimeController,
                bodyTarget = bodyTarget,
                leftHandTarget = leftHandTarget,
                rightHandTarget = rightHandTarget,
                headTarget = headTarget,
                inputToMotion = _inputToMotion,
            });

            _inputToMotion.fingerAnimator = go.GetComponent<FingerAnimator>();
            animMorphEasedTarget.blendShapeProxy = go.GetComponent<VRMBlendShapeProxy>();
        }
    }
}
