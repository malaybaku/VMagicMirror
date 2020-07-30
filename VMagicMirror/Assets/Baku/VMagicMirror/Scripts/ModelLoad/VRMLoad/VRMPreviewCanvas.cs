using UnityEngine;
using VRM;
using Zenject;

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

    }
}
