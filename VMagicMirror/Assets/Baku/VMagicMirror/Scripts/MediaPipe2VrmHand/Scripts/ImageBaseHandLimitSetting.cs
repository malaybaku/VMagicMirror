using System;
using UnityEngine;

namespace Baku.VMagicMirror
{
    [CreateAssetMenu(menuName = "VMagicMirror/ImageBaseHandLimitSetting")]
    public class ImageBaseHandLimitSetting : ScriptableObject
    {
        public Vector3 leftHandRotationOffset = new Vector3(0, 90, 0f);
        public Vector3 rightHandRotationOffset = new Vector3(0, -90, 0f); 
        public HandLimitItem[] items;
        
        
        /// <summary>
        /// ある姿勢を基準とし、そこから何度までの回転を許容するか、というのを決める値
        /// </summary>
        [Serializable]
        public class HandLimitItem
        {
            /// <summary>
            /// 基準になる姿勢を表すオイラー角
            /// </summary>
            public Vector3 rotationEuler;
            
            /// <summary>
            /// 基準の角度から何度以内なら正常な手の回転とみなすか
            /// </summary>
            [Range(0, 180f)]
            public float angle = 30f;
        }
    }
}
