using System;
using System.IO;
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
            Text m_textModelTitle;
            [SerializeField]
            Text m_textModelVersion;
            [SerializeField]
            Text m_textModelAuthor;
            [SerializeField]
            Text m_textModelContact;
            [SerializeField]
            Text m_textModelReference;
            [SerializeField]
            RawImage m_thumbnail;

            [SerializeField, Header("CharacterPermission")]
            Text m_textPermissionAllowed;
            [SerializeField]
            Text m_textPermissionViolent;
            [SerializeField]
            Text m_textPermissionSexual;
            [SerializeField]
            Text m_textPermissionCommercial;
            [SerializeField]
            Text m_textPermissionOther;

            [SerializeField, Header("DistributionLicense")]
            Text m_textDistributionLicense;
            [SerializeField]
            Text m_textDistributionOther;

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
        AniLipSync.VRM.AnimMorphTarget animMorphTarget;

        HumanPoseTransfer m_loaded;

        private void Start()
        {
            m_texts.Start();
        }

        private void Update()
        {
            //note: ここでやらんでWPFの指示でオンオフすべきでは
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Root?.SetActive(!Root.activeSelf);
            }
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
            var ext = Path.GetExtension(path).ToLower();
            switch (ext)
            {
                case ".vrm":
                    {
                        var context = new VRMImporterContext();
                        var file = File.ReadAllBytes(path);
                        context.ParseGlb(file);
                        m_texts.UpdateMeta(context);
                        context.Load();
                        context.ShowMeshes();
                        context.EnableUpdateWhenOffscreen();
                        context.ShowMeshes();
                        SetModel(context.Root);
                        break;
                    }

                //case ".glb":
                //    {
                //        var context = new UniGLTF.ImporterContext();
                //        var file = File.ReadAllBytes(path);
                //        context.ParseGlb(file);
                //        context.Load();
                //        context.ShowMeshes();
                //        context.EnableUpdateWhenOffscreen();
                //        context.ShowMeshes();
                //        SetModel(context.Root);
                //        break;
                //    }

                default:
                    Debug.LogWarningFormat("unknown file type: {0}", path);
                    break;
            }
        }

        void SetModel(GameObject go)
        {
            // cleanup
            var loaded = m_loaded;
            m_loaded = null;

            if (loaded != null)
            {
                Debug.LogFormat("destroy {0}", loaded);
                //多分コレやらないとAniLipSyncが破棄済みオブジェクトを触りに行ってしまうので。
                animMorphTarget.blendShapeProxy = null;
                GameObject.Destroy(loaded.gameObject);
            }

            if (go != null)
            {
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
                animMorphTarget.blendShapeProxy = go.GetComponent<VRMBlendShapeProxy>();
            }
        }
    }
}
