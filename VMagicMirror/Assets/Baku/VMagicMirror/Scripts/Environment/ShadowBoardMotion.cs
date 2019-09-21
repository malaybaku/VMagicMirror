﻿using UnityEngine;

namespace Baku.VMagicMirror
{
    public class ShadowBoardMotion : MonoBehaviour
    {
        [SerializeField]
        Transform cam = null;

        public float ShadowBoardWaistDepthOffset { get; set; } = 0.4f;

        void Update()
        {
            //コード通りだが、以下のうち奥側に影の影ポリが来るようにしたい
            // - 腰よりちょっと奥 : 正面～浅く見下ろした角度ではコレを使いたい
            // - 足元 : 深く見下ろした角度ではコレを使いたい
            float depthByWaist = 
                cam.transform.InverseTransformPoint(new Vector3(0, 1, 0)).z +
                ShadowBoardWaistDepthOffset;

            float depthByFoot = cam.transform.InverseTransformPoint(Vector3.zero).z;

            transform.localPosition = Mathf.Max(depthByWaist, depthByFoot) * Vector3.forward;
        }
    }
}
