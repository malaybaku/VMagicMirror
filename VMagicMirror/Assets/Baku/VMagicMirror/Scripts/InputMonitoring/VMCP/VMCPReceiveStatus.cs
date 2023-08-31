using System;
using UnityEngine;

namespace Baku.VMagicMirror.VMCP
{
    [Serializable]
    public struct VMCPReceiveStatus
    {
        [SerializeField] private bool[] connected;

        public VMCPReceiveStatus(bool[] connected)
        {
            this.connected = connected;
        }

        public string ToJson() => JsonUtility.ToJson(this);
    }
}
