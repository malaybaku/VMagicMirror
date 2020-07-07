using UnityEngine;
using VRM;
using VRMLoader;
using Zenject;

namespace Baku.VMagicMirror
{
    public class VRMPreviewCanvas : MonoBehaviour
    {
        [SerializeField] private Canvas canvasPrefab = null;
    
        private Canvas _canvas;
        private VRMPreviewUISupport _uiSupport;
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
            _uiSupport = canvasPrefab.GetComponentInChildren<VRMPreviewUISupport>();
            _uiSupport.ButtonOpenOtherPermissionUrl
                .onClick
                .AddListener(() => Application.OpenURL(_otherPermissionUrl));
            _uiSupport.ButtonOpenOtherLicenseUrl
                .onClick
                .AddListener(() => Application.OpenURL(_otherLicenseUrl));
            
        }

        public void Show(VRMImporterContext context)
        {
            CreateCanvasIfNotExist();
            
            var meta = context.ReadMeta(true);
            _otherPermissionUrl = meta.OtherPermissionUrl ?? "";
            _otherLicenseUrl = meta.OtherLicenseUrl ?? "";
            _uiSupport.ButtonOpenOtherPermissionUrl.interactable = !string.IsNullOrEmpty(_otherPermissionUrl);
            _uiSupport.ButtonOpenOtherLicenseUrl.interactable = !string.IsNullOrEmpty(_otherLicenseUrl);

            //サムネが無いVRMをロードするとき、前回のサムネが残っちゃうのを防ぐ
            _uiSupport.ResetThumbnail();

            _canvas
                .GetComponent<VRMPreviewLocale>()
                .SetLocale(
                    LanguageNameToLocaleName(_previewLanguage.Language)
                    );
            
            _canvas
                .GetComponent<VRMPreviewUI>()
                .setMeta(meta);

            _canvas.enabled = true;
            _canvas.gameObject.SetActive(true);
        }

        public void Hide() => _canvas.gameObject.SetActive(false);

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
