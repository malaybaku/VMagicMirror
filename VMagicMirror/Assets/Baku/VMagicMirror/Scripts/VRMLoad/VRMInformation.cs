using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRM;

namespace Baku.VMagicMirror
{
    public class VRMInformation : MonoBehaviour
    {
        [SerializeField]
        private Canvas canvas = null;

        [SerializeField, Header("Info")]
        private TextMeshProUGUI textModelTitle = null;
        [SerializeField]
        private TextMeshProUGUI textModelVersion = null;
        [SerializeField]
        private TextMeshProUGUI textModelAuthor = null;
        [SerializeField]
        private TextMeshProUGUI textModelContact = null;
        [SerializeField]
        private TextMeshProUGUI textModelReference = null;
        [SerializeField]
        private RawImage thumbnail = null;

        [SerializeField, Header("CharacterPermission")]
        private TextMeshProUGUI textPermissionAllowed = null;
        [SerializeField]
        private TextMeshProUGUI textPermissionViolent = null;
        [SerializeField]
        private TextMeshProUGUI textPermissionSexual = null;
        [SerializeField]
        private TextMeshProUGUI textPermissionCommercial = null;
        [SerializeField]
        private TextMeshProUGUI textPermissionOther = null;

        [SerializeField, Header("DistributionLicense")]
        private TextMeshProUGUI textDistributionLicense = null;
        [SerializeField]
        private TextMeshProUGUI textDistributionOther = null;

        void Start()
        {
            textModelTitle.text = "";
            textModelVersion.text = "";
            textModelAuthor.text = "";
            textModelContact.text = "";
            textModelReference.text = "";

            textPermissionAllowed.text = "";
            textPermissionViolent.text = "";
            textPermissionSexual.text = "";
            textPermissionCommercial.text = "";
            textPermissionOther.text = "";

            textDistributionLicense.text = "";
            textDistributionOther.text = "";
        }

        public void ShowMetaData(VRMImporterContext context)
        {
            canvas.enabled = true;

            var meta = context.ReadMeta(true);

            textModelTitle.text = meta.Title;
            textModelVersion.text = meta.Version;
            textModelAuthor.text = meta.Author;
            textModelContact.text = meta.ContactInformation;
            textModelReference.text = meta.Reference;

            textPermissionAllowed.text = meta.AllowedUser.ToString();
            textPermissionViolent.text = meta.ViolentUssage.ToString();
            textPermissionSexual.text = meta.SexualUssage.ToString();
            textPermissionCommercial.text = meta.CommercialUssage.ToString();
            textPermissionOther.text = meta.OtherPermissionUrl;

            textDistributionLicense.text = meta.LicenseType.ToString();
            textDistributionOther.text = meta.OtherLicenseUrl;

            thumbnail.texture = meta.Thumbnail;
        }

        public void Hide()
        {
            canvas.enabled = false;
        }

    }
}
