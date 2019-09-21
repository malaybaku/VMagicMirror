using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror
{
    public class VrmPreviewUISupport : MonoBehaviour
    {
        [SerializeField]
        private RawImage thumbnailImage = null;

        [SerializeField]
        private Button buttonOpenOtherPermissionUrl = null;

        [SerializeField]
        private Button buttonOpenOtherLicenseUrl = null;

        public Button ButtonOpenOtherPermissionUrl => buttonOpenOtherPermissionUrl;
        public Button ButtonOpenOtherLicenseUrl => buttonOpenOtherLicenseUrl;

        public void ResetThumbnail()
        {
            thumbnailImage.texture = null;
        }

    }
}
