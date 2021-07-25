using UnityEngine;
using UnityEngine.UI;

namespace Baku.VMagicMirror
{
    public class PerfectSyncValueIndicatorItem : MonoBehaviour
    {
        [SerializeField] private Text keyName;
        [SerializeField] private Image fillImage;

        public void SetBlendShapeName(string blendShapeName) => keyName.text = blendShapeName;
        public void SetValue(float value) => fillImage.fillAmount = value;
    }
}
