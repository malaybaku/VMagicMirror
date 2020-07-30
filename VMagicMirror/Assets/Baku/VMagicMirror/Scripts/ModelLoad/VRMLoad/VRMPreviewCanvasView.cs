using UnityEngine;
using VRMLoader;

namespace Baku.VMagicMirror
{
    /// <summary> Prefab越しで静的にコンポーネントを引き渡すための、グルーっぽいMonoBehaviour </summary>
    public class VRMPreviewCanvasView : MonoBehaviour
    {
        [SerializeField] private VRMPreviewUI _previewUi = null;
        [SerializeField] private VRMPreviewLocale _locale = null;
        [SerializeField] private VRMPreviewUISupport _previewUiSupport = null;

        public VRMPreviewUI PreviewUI => _previewUi;
        public VRMPreviewLocale Locale => _locale;
        public VRMPreviewUISupport UISupport => _previewUiSupport;
    }
}
