using UnityEngine;

namespace Baku.VMagicMirror
{
    [CreateAssetMenu(menuName = "VMagicMirror/ImageBaseHandLimitSetting")]
    public class ImageBaseHandLimitSetting : ScriptableObject
    {
        //NOTE: y,zは左手では反転して使う
        [Header("右手を基準に指定します")]
        public Vector3 eulerMinAngles = new Vector3(-10, -80, -40);
        public Vector3 eulerMaxAngles = new Vector3(80, -5, 40);
        
        public Vector3 leftHandRotationOffset = new Vector3(0, 90, 0f);
        public Vector3 rightHandRotationOffset = new Vector3(0, -90, 0f);
    }
}
