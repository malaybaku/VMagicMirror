using System;
using TMPro;
using UniGLTF.Extensions.VRMC_vrm;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror
{
    public enum PreviewUILocale
    {
        Japanese,
        English,
    }

    /// <summary> VRM1.0用のメタ情報を表示するUI </summary>
    public class VRM10MetaView : MonoBehaviour
    {
        //TODO: テキストのハードコーディング避けるのは考えてもいいが、
        //分量的にハードコーディングでも許せる + 3言語以上サポートするモチベが低め

        //NOTE: 理由があればTMPでもいいです、ぜんぶ
        [Serializable]
        class Headers
        {
            //要らんものはprefab側でobjectを消したりしてもよい
            [SerializeField] private TextMeshProUGUI title;
            [SerializeField] private TextMeshProUGUI version;
            [SerializeField] private TextMeshProUGUI authors;
            [SerializeField] private TextMeshProUGUI copyright;
            [SerializeField] private TextMeshProUGUI contactInformation;
            [SerializeField] private TextMeshProUGUI references;
            [SerializeField] private TextMeshProUGUI thirdPartyLicenses;
            [SerializeField] private TextMeshProUGUI licenseUrl;
            [SerializeField] private TextMeshProUGUI usageHeader;
            [SerializeField] private TextMeshProUGUI avatarPermission;
            [SerializeField] private TextMeshProUGUI allowExcessiveViolentUsage;
            [SerializeField] private TextMeshProUGUI allowExcessiveSexualUsage;
            [SerializeField] private TextMeshProUGUI allowPoliticalOrReligiousUsage;
            [SerializeField] private TextMeshProUGUI allowAntisocialOrHateUsage;
            [SerializeField] private TextMeshProUGUI creditNotation;
            [SerializeField] private TextMeshProUGUI allowRedistribution;
            [SerializeField] private TextMeshProUGUI modification;
            [SerializeField] private TextMeshProUGUI commercialUsage;
            [SerializeField] private TextMeshProUGUI otherLicenseUrl;

            public void SetLocale(PreviewUILocale locale)
            {
                switch (locale)
                {
                    case PreviewUILocale.Japanese:
                        title.text = "モデル名: ";
                        version.text = "バージョン: ";
                        authors.text = "製作者: ";
                        copyright.text = "Copyright: ";
                        contactInformation.text = "連絡先: ";
                        references.text = "関連情報: ";
                        thirdPartyLicenses.text = "サードパーティライセンス: ";
                        licenseUrl.text = "ライセンスURL: ";

                        usageHeader.text = "利用方法";
                        avatarPermission.text = "アバターとしての使用: ";
                        allowExcessiveViolentUsage.text = "過剰な暴力表現: ";
                        allowExcessiveSexualUsage.text = "過剰な性的表現: ";
                        allowPoliticalOrReligiousUsage.text = "政治・宗教的使用: ";
                        allowAntisocialOrHateUsage.text = "反社会的・差別的表現: ";
                        commercialUsage.text = "商用利用: ";
                        creditNotation.text = "クレジット表記: ";
                        allowRedistribution.text = "再配布: ";
                        modification.text = "モデル改変および再配布: ";
                        
                        otherLicenseUrl.text = "その他のライセンスURL: ";
                        break;
                    case PreviewUILocale.English:
                    default:
                        title.text = "Name: ";
                        version.text = "Version: ";
                        authors.text = "Authors: ";
                        copyright.text = "Copyright: ";
                        contactInformation.text = "Contact: ";
                        references.text = "Reference: ";
                        thirdPartyLicenses.text = "3rd Party License: ";
                        licenseUrl.text = "License URL: ";

                        usageHeader.text = "Allowed Usages:";

                        avatarPermission.text = "Use as Avatar: ";
                        allowExcessiveViolentUsage.text = "Excessively Violent: ";
                        allowExcessiveSexualUsage.text = "Excessively Sexual: ";
                        allowPoliticalOrReligiousUsage.text = "Political or Religious: ";
                        allowAntisocialOrHateUsage.text = "Antisocial or Hate: ";
                        commercialUsage.text = "Commercial Usage: ";
                        
                        creditNotation.text = "Credit Notation: ";
                        allowRedistribution.text = "Redistribution: ";
                        modification.text = "Modification: ";
                        
                        otherLicenseUrl.text = "Other License URL: ";
                        break;
                        
                    //TODO: English
                }
            }
        }

        [Serializable]
        class Inputs
        {
            [SerializeField] private TextMeshProUGUI title;
            [SerializeField] private TextMeshProUGUI version;
            [SerializeField] private TextMeshProUGUI authors;
            [SerializeField] private TextMeshProUGUI copyright;
            [SerializeField] private TextMeshProUGUI contactInformation;
            [SerializeField] private TextMeshProUGUI references;

            [SerializeField] private TextMeshProUGUI avatarPermission;
            [SerializeField] private TextMeshProUGUI allowExcessiveViolentUsage;
            [SerializeField] private TextMeshProUGUI allowExcessiveSexualUsage;
            [SerializeField] private TextMeshProUGUI allowPoliticalOrReligiousUsage;
            [SerializeField] private TextMeshProUGUI allowAntisocialOrHateUsage;
            [SerializeField] private TextMeshProUGUI creditNotation;
            [SerializeField] private TextMeshProUGUI allowRedistribution;
            [SerializeField] private TextMeshProUGUI modification;
            [SerializeField] private TextMeshProUGUI commercialUsage;

            [SerializeField] private VRM10MetaLicenseItemView licenseUrlItem;
            [SerializeField] private VRM10MetaLicenseItemView thirdPartyLicenseUrlItem;
            [SerializeField] private VRM10MetaLicenseItemView otherLicenseUrlItem;

            public Observable<string> OpenUrlRequested => Observable.Merge(
                licenseUrlItem.OpenUrlRequested,
                thirdPartyLicenseUrlItem.OpenUrlRequested,
                otherLicenseUrlItem.OpenUrlRequested
            );

            //NOTE: enumの内容をlocaleと突き合わせて反映したりする
            public void Update(Meta meta, PreviewUILocale locale)
            {
                title.text = meta.Name;
                version.text = meta.Version;
                authors.text = string.Join(", ", meta.Authors);
                copyright.text = meta.CopyrightInformation ?? "";
                contactInformation.text = meta.ContactInformation ?? "";
                references.text = (meta.References != null && meta.References.Count > 0)
                    ? string.Join(", ", meta.References)
                    : "";
                
                avatarPermission.text = GetAvatarPermissionString(meta.AvatarPermission, locale);
                allowExcessiveViolentUsage.text = 
                    GetUsageAllowString(meta.AllowExcessivelyViolentUsage == true, locale);
                allowExcessiveViolentUsage.color =
                    GetUsageAllowColor(meta.AllowExcessivelyViolentUsage == true);
                allowExcessiveSexualUsage.text =
                    GetUsageAllowString(meta.AllowExcessivelySexualUsage == true, locale);
                allowExcessiveSexualUsage.color =
                    GetUsageAllowColor(meta.AllowExcessivelySexualUsage == true);
                allowPoliticalOrReligiousUsage.text =
                    GetUsageAllowString(meta.AllowPoliticalOrReligiousUsage == true, locale);
                allowPoliticalOrReligiousUsage.color =
                    GetUsageAllowColor(meta.AllowPoliticalOrReligiousUsage == true);
                allowAntisocialOrHateUsage.text =
                    GetUsageAllowString(meta.AllowAntisocialOrHateUsage == true, locale);
                allowAntisocialOrHateUsage.color =
                    GetUsageAllowColor(meta.AllowAntisocialOrHateUsage == true);

                creditNotation.text = 
                    GetRequiredBoolString(meta.CreditNotation == CreditNotationType.required, locale);
                allowRedistribution.text =
                    GetUsageAllowString(meta.AllowRedistribution == true, locale);
                allowRedistribution.color =
                    GetUsageAllowColor(meta.AllowRedistribution == true);
                commercialUsage.text = GetCommercialUsageString(meta.CommercialUsage, locale);
                modification.text = GetModificationPermissionString(meta.Modification, locale);
                
                licenseUrlItem.SetUrl(meta.LicenseUrl);
                thirdPartyLicenseUrlItem.SetUrl(meta.ThirdPartyLicenses);
                otherLicenseUrlItem.SetUrl(meta.OtherLicenseUrl);
            }

            private static readonly Color _allowedColor = new Color(3 / 255f, 175 / 255f, 122 / 255f);
            private static readonly Color _prohibitedColor = new Color(1f, 75 / 255f, 0f);

            private static string GetAvatarPermissionString(AvatarPermissionType type, PreviewUILocale locale)
            {
                switch (locale)
                {
                    case PreviewUILocale.Japanese:
                        switch (type)
                        {
                            case AvatarPermissionType.everyone: return "誰でも";
                            case AvatarPermissionType.onlySeparatelyLicensedPerson: return "別途許可された人のみ";
                            case AvatarPermissionType.onlyAuthor:
                            default:
                                return "作者のみ";
                        }
                    case PreviewUILocale.English:
                    default : 
                        switch (type)
                        {
                            case AvatarPermissionType.everyone: return "Everyone";
                            case AvatarPermissionType.onlySeparatelyLicensedPerson: return "Only Licensed Person";
                            case AvatarPermissionType.onlyAuthor:
                            default:
                                return "Only Author";
                        }
                }
            }

            private static string GetCommercialUsageString(CommercialUsageType type, PreviewUILocale locale)
            {
                switch (locale)
                {
                    case PreviewUILocale.Japanese:
                        switch (type)
                        {
                            case CommercialUsageType.personalProfit: return "個人の商用利用";
                            case CommercialUsageType.corporation: return "個人・法人の利用";
                            case CommercialUsageType.personalNonProfit:
                            default:
                                return "個人の非商用利用";
                        }
                    case PreviewUILocale.English:
                    default : 
                        switch (type)
                        {
                            case CommercialUsageType.personalProfit: return "Personal Profit";
                            case CommercialUsageType.corporation: return "Personal or Corporation";
                            case CommercialUsageType.personalNonProfit:
                            default:
                                return "Personal Non-Profit";
                        }
                }
            }

            private static string GetModificationPermissionString(ModificationType type, PreviewUILocale locale)
            {
                switch (locale)
                {
                    case PreviewUILocale.Japanese:
                        switch (type)
                        {
                            case ModificationType.allowModificationRedistribution: return "改変して再配布可";
                            case ModificationType.allowModification: return "改変可・再配布は禁止";
                            case ModificationType.prohibited:
                            default:
                                return "不許可";
                        }
                    case PreviewUILocale.English:
                    default : 
                        switch (type)
                        {
                            case ModificationType.allowModificationRedistribution:
                                return "Can be modified and redistributed";
                            case ModificationType.allowModification:
                                return "Can be modified, but redistribution prohibited";
                            case ModificationType.prohibited:
                            default:
                                return "Prohibited";
                        }
                }                
            }
            
            private static string GetUsageAllowString(bool isAllowed, PreviewUILocale locale)
            {
                //シンプルでいい気がしたので…
                return isAllowed ? "OK" : "NG";
            }

            private static Color GetUsageAllowColor(bool isAllowed)
            {
                //NOTE: 別に文化的正しさとかは考えなくてよく、色が違ってメリハリがついてればよい
                return isAllowed ? _allowedColor : _prohibitedColor;
            }
            
            private static string GetRequiredBoolString(bool required, PreviewUILocale locale)
            {
                switch (locale)
                {
                    case PreviewUILocale.Japanese:
                        return required ? "必須" : "不要";
                    case PreviewUILocale.English:
                    default:
                        return required ? "Required" : "Unnecessary";
                }
            }

            private static string ValueOrNone(string value)
            {
                return string.IsNullOrEmpty(value) ? "(none)" : value;
            }
        }

        [SerializeField] private Headers headers;
        [SerializeField] private Inputs inputs;
        [SerializeField] private RawImage thumbnailImage;

        //NOTE: デフォルト値が無くても大丈夫ならそれはそれでOK
        private PreviewUILocale _locale = PreviewUILocale.English;
        private Meta _meta = null;

        public Observable<string> OpenUrlRequested => inputs.OpenUrlRequested;

        public void SetMeta(Meta metaData)
        {
            _meta = metaData;
            inputs.Update(metaData, _locale);
        }

        //nullも許可: テクスチャの破棄直前にはnullが来るのを期待
        public void SetThumbnail(Texture2D thumbnail)
        {
            thumbnailImage.texture = thumbnail;
            thumbnailImage.gameObject.SetActive(thumbnail != null);
        }

        public void SetLocale(PreviewUILocale locale)
        {
            _locale = locale;
            headers.SetLocale(locale);
            if (_meta != null)
            {
                inputs.Update(_meta, locale);
            }
        }

        public void SetActive(bool active) => gameObject.SetActive(active);
    }
}
