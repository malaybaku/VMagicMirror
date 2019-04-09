using System;
using UnityEngine;
using TMPro;
using UniRx;

namespace Baku.VMagicMirror
{
    public class VRMPreviewLanguage : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        [SerializeField]
        private Texts texts;

        public string Language { get; private set; } = "Japanese";

        private void Start()
        {
            texts.SetLanguage("Japanese");
            handler.Commands.Subscribe(message =>
            {
                if (message.Command == MessageCommandNames.Language)
                {
                    SetLanguage(message.Content);
                }
            });
        }

        private void SetLanguage(string languageName)
        {
            Language = languageName;
            texts.SetLanguage(languageName);
        }

        [Serializable]
        struct Texts
        {
            [SerializeField, Header("Info")]
            TextMeshProUGUI textInfoHeader;
            [SerializeField]
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
            TextMeshProUGUI m_textModelThumbnail;

            [SerializeField, Header("License")]
            TextMeshProUGUI m_textLicenseHeader;

            [SerializeField, Header("CharacterPermission")]
            TextMeshProUGUI permissionHeader;
            [SerializeField]
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
            TextMeshProUGUI textDistributionHeader;
            [SerializeField]
            TextMeshProUGUI m_textDistributionLicense;
            [SerializeField]
            TextMeshProUGUI m_textDistributionOther;

            //量が大したことないのでベタ打ちで…
            public void SetLanguage(string languageName)
            {
                switch (languageName)
                {
                    case "Japanese":
                        textInfoHeader.text = "モデル情報";
                        m_textModelTitle.text = "タイトル";
                        m_textModelVersion.text = "バージョン";
                        m_textModelAuthor.text = "作者";
                        m_textModelContact.text = "連絡先";
                        m_textModelReference.text = "参照";
                        m_textModelThumbnail.text = "サムネイル";

                        m_textLicenseHeader.text = "ライセンス";
                        permissionHeader.text = "アバターの人格に関する許諾範囲";
                        m_textPermissionAllowed.text = "許諾範囲";
                        m_textPermissionViolent.text = "暴力表現";
                        m_textPermissionSexual.text = "性的表現";
                        m_textPermissionCommercial.text = "商用利用";
                        m_textPermissionOther.text = "その他の条件";

                        textDistributionHeader.text = "再配布・改変に関する許諾範囲";
                        m_textDistributionLicense.text = "ライセンス";
                        m_textDistributionOther.text = "その他の条件";
                        break;
                    case "English":
                    default:
                        textInfoHeader.text = "Model Information";
                        m_textModelTitle.text = "Title";
                        m_textModelVersion.text = "Version";
                        m_textModelAuthor.text = "Author";
                        m_textModelContact.text = "Contact";
                        m_textModelReference.text = "Reference";
                        m_textModelThumbnail.text = "Thumbnail";

                        m_textLicenseHeader.text = "License";
                        permissionHeader.text = "Personation / Charaterization Permission";
                        m_textPermissionAllowed.text = "Avatar Permission";
                        m_textPermissionViolent.text = "Violent Acts";
                        m_textPermissionSexual.text = "Sexual Acts";
                        m_textPermissionCommercial.text = "Commercial use";
                        m_textPermissionOther.text = "License Url";

                        textDistributionHeader.text = "Redistribution / Modifications License";
                        m_textDistributionLicense.text = "License";
                        m_textDistributionOther.text = "License Url";
                        break;
                }
            }
        }

    }
}
