using UnityEngine;
using VRM;
using VRMLoader;

namespace Baku.VMagicMirror
{
    //NOTE: 表示するキャンバスに貼り付ける前提のスクリプトであることに注意
    [RequireComponent(typeof(Canvas))]
    public class VRMPreviewCanvas : MonoBehaviour
    {
        // define VRMLoaderUI/Prefabs/LoadConfirmModal
        [SerializeField]
        GameObject m_modalWindow;

        [SerializeField]
        VRMPreviewLanguage m_previewLanguage = null;

        Canvas m_canvas;

        VRMPreviewUISupport uiSupport = null;

        private string _otherPermissionUrl = "";
        private string _otherLicenseUrl = "";

        private void Start()
        {
            m_canvas = GetComponent<Canvas>();

            uiSupport = m_modalWindow.GetComponentInChildren<VRMPreviewUISupport>();
            uiSupport.ButtonOpenOtherPermissionUrl
                .onClick
                .AddListener(() => Application.OpenURL(_otherPermissionUrl));
            uiSupport.ButtonOpenOtherLicenseUrl
                .onClick
                .AddListener(() => Application.OpenURL(_otherLicenseUrl));
        }

        public void Show(VRMImporterContext context)
        {
            var meta = context.ReadMeta(true);

            _otherPermissionUrl = meta.OtherPermissionUrl ?? "";
            _otherLicenseUrl = meta.OtherLicenseUrl ?? "";
            uiSupport.ButtonOpenOtherPermissionUrl.interactable = !string.IsNullOrEmpty(_otherPermissionUrl);
            uiSupport.ButtonOpenOtherLicenseUrl.interactable = !string.IsNullOrEmpty(_otherLicenseUrl);

            //サムネが無いVRMをロードするとき、前回のサムネが残っちゃうのを防ぐ
            uiSupport.ResetThumbnail();

            m_modalWindow
                .GetComponent<VRMPreviewLocale>()
                .SetLocale(
                    LanguageNameToLocaleName(m_previewLanguage.Language)
                    );
            
            m_modalWindow
                .GetComponent<VRMPreviewUI>()
                .setMeta(meta);

            m_canvas.enabled = true;
        }

        public void Hide()
        {
            m_canvas.enabled = false;
        }

        private string LanguageNameToLocaleName(string languageName)
        {
            switch (languageName)
            {
                case "Japanese":
                    return "ja";
                case "English":
                default:
                    return "en";
            }
        }

    }
}
