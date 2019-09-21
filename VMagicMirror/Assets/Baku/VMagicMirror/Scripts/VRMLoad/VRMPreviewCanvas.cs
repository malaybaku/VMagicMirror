using UnityEngine;
using VRM;
using VRMLoader;

namespace Baku.VMagicMirror
{
    //NOTE: 表示するキャンバスに貼り付ける前提のスクリプトであることに注意
    [RequireComponent(typeof(Canvas))]
    public class VrmPreviewCanvas : MonoBehaviour
    {
        // define VRMLoaderUI/Prefabs/LoadConfirmModal
        [SerializeField] private GameObject modalWindow;

        [SerializeField] private VrmPreviewLanguage previewLanguage;

        private Canvas _canvas;
        private VrmPreviewUISupport _uiSupport;

        private string _otherPermissionUrl = "";
        private string _otherLicenseUrl = "";

        private void Start()
        {
            _canvas = GetComponent<Canvas>();

            _uiSupport = modalWindow.GetComponentInChildren<VrmPreviewUISupport>();
            _uiSupport.ButtonOpenOtherPermissionUrl
                .onClick
                .AddListener(() => Application.OpenURL(_otherPermissionUrl));
            _uiSupport.ButtonOpenOtherLicenseUrl
                .onClick
                .AddListener(() => Application.OpenURL(_otherLicenseUrl));
        }

        public void Show(VRMImporterContext context)
        {
            var meta = context.ReadMeta(true);

            _otherPermissionUrl = meta.OtherPermissionUrl ?? "";
            _otherLicenseUrl = meta.OtherLicenseUrl ?? "";
            _uiSupport.ButtonOpenOtherPermissionUrl.interactable = !string.IsNullOrEmpty(_otherPermissionUrl);
            _uiSupport.ButtonOpenOtherLicenseUrl.interactable = !string.IsNullOrEmpty(_otherLicenseUrl);

            //サムネが無いVRMをロードするとき、前回のサムネが残っちゃうのを防ぐ
            _uiSupport.ResetThumbnail();

            modalWindow
                .GetComponent<VRMPreviewLocale>()
                .SetLocale(
                    LanguageNameToLocaleName(previewLanguage.Language)
                    );
            
            modalWindow
                .GetComponent<VRMPreviewUI>()
                .setMeta(meta);

            _canvas.enabled = true;
        }

        public void Hide()
        {
            _canvas.enabled = false;
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
