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
        
        //NOTE: ホンモノじゃなくて良いがHipsっぽいTransformを入れておく
        [SerializeField] private Transform referenceHips;

        [SerializeField] private VMCPReceiveDebuggerToModel[] modelSetters;
        
        [SerializeField] private Transform headTracker;
        [SerializeField] private Transform leftHandTracker;
        [SerializeField] private Transform rightHandTracker;

        private uOscServer _server;
        private VMCPBasedHumanoid _vmcpHumanoid = new VMCPBasedHumanoid();
        
        private void Start()
        {
            _server = GetComponent<uOscServer>();
            _server.onDataReceived.AddListener(OnOscMessageReceived);
            
            _vmcpHumanoid.GenerateHumanoidBoneHierarchy();
        }

        private void Update()
        {
            //NOTE: 初期位置から体全体が左右に動く」をホントは検出したいんだけど、キャリブ操作を省いてそうできるかって話なんだよな…
            var hipsRot = Quaternion.Euler(0, referenceHips.rotation.eulerAngles.y, 0);
            var hipsPose = new Pose(referenceHips.position, hipsRot);
            
            //受信設定を無視する(めんどくさいので…)
            SetPositionAndRotation(headTracker, hipsPose, _vmcpHumanoid.GetFKHeadPoseFromHips());
            SetPositionAndRotation(leftHandTracker, hipsPose, _vmcpHumanoid.GetFKLeftHandPoseFromHips());
            SetPositionAndRotation(rightHandTracker, hipsPose, _vmcpHumanoid.GetFKRightHandPoseFromHips());

            foreach (var setter in modelSetters)
            {
                setter.SetFingerFK(_vmcpHumanoid);
                setter.SetHeadIK(GetPose(headTracker));
                setter.SetLefHandIK(GetPose(leftHandTracker));
                setter.SetRightHandIK(GetPose(rightHandTracker));
            }
        }

        private Pose GetPose(Transform t) => new Pose(t.position, t.rotation);
        
        private void SetPositionAndRotation(Transform target, Pose basePose, Pose localPose)
        {
            target.SetPositionAndRotation(
                basePose.position + basePose.rotation * localPose.position,
                basePose.rotation * localPose.rotation
            );
        }
        
        private void OnOscMessageReceived(uOSC.Message message)
        {
            var messageType = message.GetMessageType();
            
            //VMCPHumanoidへ格納する処理は常に走る
            switch (messageType)
            {
                case VMCPMessageType.ForwardKinematics:
                    SetFKToHumanoid(message);
                    break;
            }

            foreach (var setting in settings)
            {
                if (setting.MessageType != messageType)
                {
                    continue;
                }

                switch (messageType)
                {
                    // case VMCPMessageType.DefineRoot:
                    //     Debug.Log($"Receive Define Root, but not supported so far...");
                    //     break;
                    // case VMCPMessageType.ForwardKinematics:
                    //     Debug.Log($"Receive FK data, but not supported so far...");
                    //     break;
                    // case VMCPMessageType.TrackerPose:
                    //     Debug.Log($"Receive tracker pose, type={message.GetTrackerPoseType(out _)}");
                    //     if (setting.TrackerPoseType == message.GetTrackerPoseType())
                    //     {
                    //         ApplyPose(setting, message);
                    //     }
                    //     break;
                    case VMCPMessageType.BlendShapeValue:
                        OnReceiveBlendShapeValue(setting, message);
                        break;
                    case VMCPMessageType.BlendShapeApply:
                        OnReceiveBlendShapeApply(setting, message);
                        break;
                }
            }
        }

        private void SetFKToHumanoid(uOSC.Message message)
        {
            var boneName = message.GetForwardKinematicBoneName();
            if (string.IsNullOrEmpty(boneName))
            {
                return;
            }
            var pose = message.GetPose();
            _vmcpHumanoid.SetLocalPose(boneName, pose.pos, pose.rot);
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
