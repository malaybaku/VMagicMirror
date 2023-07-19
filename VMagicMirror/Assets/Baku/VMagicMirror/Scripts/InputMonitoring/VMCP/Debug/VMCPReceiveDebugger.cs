using System;
using UnityEngine;
using uOSC;

namespace Baku.VMagicMirror.VMCP
{
    /// <summary>
    /// 単発シーンでお試しでVMCPの受信をやるためのコード。
    /// PoCをしたり、他アプリの挙動を見るために使っている。(本来はこういうの書かないでもデバッグ用のVMCPツールはあるが)
    /// </summary>
    [RequireComponent(typeof(uOscServer))]
    public class VMCPReceiveDebugger : MonoBehaviour
    {
        [Serializable]
        class DataReceiveSetting
        {
            [SerializeField] private VMCPMessageType messageType;
            [SerializeField] private VMCPTrackerPoseType trackerPoseType;
            [SerializeField] private Transform trackerApplyTarget;
            [SerializeField] private bool writeLog;

            public VMCPMessageType MessageType => messageType;
            public VMCPTrackerPoseType TrackerPoseType => trackerPoseType;
            public Transform TrackerApplyTarget => trackerApplyTarget;
            public bool WriteLog => writeLog;
        }
        
        [SerializeField] private DataReceiveSetting[] settings;

        private uOscServer _server;
        
        private void Start()
        {
            _server = GetComponent<uOscServer>();
            _server.onDataReceived.AddListener(OnOscMessageReceived);
        }

        private void OnOscMessageReceived(uOSC.Message message)
        {
            var messageType = message.GetMessageType();
            foreach (var setting in settings)
            {
                if (setting.MessageType != messageType)
                {
                    continue;
                }

                switch (messageType)
                {
                    case VMCPMessageType.DefineRoot:
                        Debug.Log($"Receive Define Root, but not supported so far...");
                        break;
                    case VMCPMessageType.ForwardKinematics:
                        Debug.Log($"Receive FK data, but not supported so far...");
                        break;
                    case VMCPMessageType.TrackerPose:
                        Debug.Log($"Receive tracker pose, type={message.GetTrackerPoseType()}");
                        if (setting.TrackerPoseType == message.GetTrackerPoseType())
                        {
                            ApplyPose(setting, message);
                        }
                        break;
                    case VMCPMessageType.BlendShapeValue:
                        OnReceiveBlendShapeValue(setting, message);
                        break;
                    case VMCPMessageType.BlendShapeApply:
                        OnReceiveBlendShapeApply(setting, message);
                        break;
                }
            }
        }

        private void ApplyPose(DataReceiveSetting setting, uOSC.Message message)
        {
            var (pos, rot) = message.GetPose();
            if (setting.TrackerApplyTarget != null)
            {
                setting.TrackerApplyTarget.localPosition = pos;
                setting.TrackerApplyTarget.localRotation = rot;
            }
            
            if (setting.WriteLog)
            {
                Debug.Log($"VMCPReceiveDebugger: Pose {setting.TrackerPoseType}, {pos.x:0.000}, {pos.y:0.000}, {pos.z:0.000}");
            }
        }

        private void OnReceiveBlendShapeValue(DataReceiveSetting setting, uOSC.Message message)
        {
            if (!message.TryGetBlendShapeValue(out var key, out var value))
            {
                return;
            }

            if (setting.WriteLog)
            {
                Debug.Log($"VMCPReceiveDebugger: BlendShape Value {key}={value:0.00}");
            }
        }

        private void OnReceiveBlendShapeApply(DataReceiveSetting setting, uOSC.Message message)
        {
            if (setting.WriteLog)
            {
                Debug.Log("VMCPReceiveDebugger: BlendShape Apply");
            }
        }
    }
}
