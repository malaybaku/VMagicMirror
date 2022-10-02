using UnityEngine;
using UniVRM10.Migration;
using VRM;
using Zenject;
using AllowedUser = VRM.AllowedUser;
using UssageLicense = VRM.UssageLicense;
using LicenseType = VRM.LicenseType;

namespace Baku.VMagicMirror
{
    public class VRMPreviewCanvas : MonoBehaviour
    {
        [SerializeField] private VRMPreviewCanvasView canvasPrefab = null;
    
        private VRMPreviewCanvasView _canvas;
        private VRMPreviewLanguage _previewLanguage;

        private string _otherPermissionUrl = "";
        private string _otherLicenseUrl = "";

        [Inject]
        public void Initialize(VRMPreviewLanguage previewLanguage)
        {
            _previewLanguage = previewLanguage;
        }

        private void CreateCanvasIfNotExist()
        {
            if (_canvas != null)
            {
                return;
            }
            
            _canvas = Instantiate(canvasPrefab, transform);
            _canvas.UISupport
                .ButtonOpenOtherPermissionUrl
                .onClick
                .AddListener(() => Application.OpenURL(_otherPermissionUrl));
            _canvas.UISupport
                .ButtonOpenOtherLicenseUrl
                .onClick
                .AddListener(() => Application.OpenURL(_otherLicenseUrl));
        }

        public void Show(VRMImporterContext context)
        {
            CreateCanvasIfNotExist();
            
            var meta = context.ReadMeta(true);
            _otherPermissionUrl = meta.OtherPermissionUrl ?? "";
            _otherLicenseUrl = meta.OtherLicenseUrl ?? "";
            _canvas.UISupport.ButtonOpenOtherPermissionUrl.interactable = !string.IsNullOrEmpty(_otherPermissionUrl);
            _canvas.UISupport.ButtonOpenOtherLicenseUrl.interactable = !string.IsNullOrEmpty(_otherLicenseUrl);

            //サムネが無いVRMをロードするとき、前回のサムネが残っちゃうのを防ぐ
            _canvas.UISupport.ResetThumbnail();
            
            _canvas.Locale.SetLocale(LanguageNameToLocaleName(_previewLanguage.Language));
            _canvas.PreviewUI.setMeta(meta);
            _canvas.gameObject.SetActive(true);
        }

        public void Show(Vrm0Meta meta, Texture2D thumbnail)
        {
            CreateCanvasIfNotExist();
            
            //var meta = context.ReadMeta(true);
            _otherPermissionUrl = meta.otherPermissionUrl ?? "";
            _otherLicenseUrl = meta.otherLicenseUrl ?? "";
            _canvas.UISupport.ButtonOpenOtherPermissionUrl.interactable = !string.IsNullOrEmpty(_otherPermissionUrl);
            _canvas.UISupport.ButtonOpenOtherLicenseUrl.interactable = !string.IsNullOrEmpty(_otherLicenseUrl);

            //サムネが無いVRMをロードするとき、前回のサムネが残っちゃうのを防ぐ
            _canvas.UISupport.ResetThumbnail();
            
            _canvas.Locale.SetLocale(LanguageNameToLocaleName(_previewLanguage.Language));
            //ちょっと横着だが、旧UIの実装を放置できるように「一瞬作ってすぐ破棄」というスタイルにしている
            var oldMeta = CreateOldMetaObject(meta, thumbnail);
            _canvas.PreviewUI.setMeta(oldMeta);
            Destroy(oldMeta);
            _canvas.gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
            }
        }

        private static string LanguageNameToLocaleName(string languageName)
        {
            switch (languageName)
            {
                case "Japanese":
                    return "ja";
                //case "English":
                default:
                    return "en";
            }
        }

        private static VRMMetaObject CreateOldMetaObject(Vrm0Meta meta, Texture2D thumbnail)
        {
            var result = ScriptableObject.CreateInstance<VRMMetaObject>();
            result.Thumbnail = thumbnail;

            //NOTE: ExporterVersionないが、まあ不要ということで…
            result.ExporterVersion = "";
            result.Title = meta.title;
            result.Version = meta.version;
            result.Author = meta.author;
            result.ContactInformation = meta.contactInformation;
            result.Reference = meta.reference;
            result.AllowedUser = (AllowedUser) meta.allowedUser;
            result.ViolentUssage = meta.violentUsage ? UssageLicense.Allow : UssageLicense.Disallow;
            result.SexualUssage = meta.sexualUsage ? UssageLicense.Allow : UssageLicense.Disallow;
            result.CommercialUssage = meta.commercialUsage ? UssageLicense.Allow : UssageLicense.Disallow;

            result.OtherPermissionUrl = meta.otherPermissionUrl;

            result.LicenseType = (LicenseType) meta.licenseType;
            result.OtherLicenseUrl = meta.otherLicenseUrl;

            return result;
        }
        
    }
}
