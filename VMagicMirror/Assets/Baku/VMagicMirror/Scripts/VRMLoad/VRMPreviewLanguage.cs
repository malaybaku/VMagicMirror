using System;
using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror
{
    public class VRMPreviewLanguage : MonoBehaviour
    {
        public string Language { get; private set; } = "Japanese";

        [Serializable]
        struct Texts
        {
            [SerializeField, Header("Info")]
            Text textInfoHeader;
            [SerializeField]
            Text m_textModelTitle;
            [SerializeField]
            Text m_textModelVersion;
            [SerializeField]
            Text m_textModelAuthor;
            [SerializeField]
            Text m_textModelContact;
            [SerializeField]
            Text m_textModelReference;

            [SerializeField, Header("CharacterPermission")]
            Text permissionHeader;
            [SerializeField]
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
            Text textDistributionHeader;
            [SerializeField]
            Text m_textDistributionLicense;
            [SerializeField]
            Text m_textDistributionOther;

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

                        permissionHeader.text = "Personation / Charaterization Permission";
                        m_textPermissionAllowed.text = "Avatar Permission";
                        m_textPermissionViolent.text = "Violent Acts";
                        m_textPermissionSexual.text = "Sexuality Acts";
                        m_textPermissionCommercial.text = "Commercial use";
                        m_textPermissionOther.text = "Other License Url";

                        textDistributionHeader.text = "Redistribution / Modifications License";
                        m_textDistributionLicense.text = "License";
                        m_textDistributionOther.text = "Other License Url";
                        break;
                }
            }
        }

        [SerializeField]
        private Texts texts;

        private void Start()
        {
            texts.SetLanguage("Japanese");
        }

        public void SetLanguage(string languageName)
        {
            Language = languageName;
            texts.SetLanguage(languageName);
        }
    }
}
