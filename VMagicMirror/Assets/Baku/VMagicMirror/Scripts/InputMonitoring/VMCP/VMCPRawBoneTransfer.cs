using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.VMCP
{
    //NOTE: このクラスの`Set`はIKの適用より後、かつVRM1.0のRuntimeのProcess()よりは先に呼ばれる必要がある。のでMonoBehaviour。
    public class VMCPRawBoneTransfer : MonoBehaviour
    {
        //NOTE:
        // - このクラスの守備範囲はSpine ~ Headまでと両腕のShoulder ~ Handまで。
        //   - Headを送ってるSourceがSpine~Headのボーン回転を指定できる
        //   - Handを送ってるSourceがShoulder~Handのボーン回転を指定できる
        // - 指ボーンは取得できている場合、VMCPHandに基づいてVMCPBasedFingerSetterが処理するので、ここでの処理は不要
        private static readonly HumanBodyBones[] UpperBodyBones = new[]
        {
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.UpperChest,
            HumanBodyBones.Neck,
            HumanBodyBones.Head,
        };

        private static readonly HumanBodyBones[] ArmBones = new[]
        {
            HumanBodyBones.LeftShoulder,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightShoulder,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.RightLowerArm,
            HumanBodyBones.RightHand,
        };
        
        //NOTE: 腕以外でSpine ~ Head
        private readonly Dictionary<string, Transform> _upperBodyBones = new();
        //Shoulder~Handまでの両腕ぶん
        private readonly Dictionary<string, Transform> _armBones = new();

        private bool _hasModel;
        private bool _boneTransferEnabled;

        private IMessageReceiver _receiver;
        private IVRMLoadable _vrmLoadable;
        private VMCPHandPose _vmcpHand;
        private VMCPHeadPose _vmcpHead;
        
        [Inject]
        public void Initialize(
            IMessageReceiver receiver, IVRMLoadable vrmLoadable, 
            VMCPHandPose vmcpHand, VMCPHeadPose vmcpHead)
        {
            _receiver = receiver;
            _vrmLoadable = vrmLoadable;
            _vmcpHand = vmcpHand;
            _vmcpHead = vmcpHead;

            _receiver.AssignCommandHandler(
                VmmCommands.SetVMCPNaiveBoneTransfer,
                command => _boneTransferEnabled = command.ToBoolean()
                );
            
            _vrmLoadable.VrmLoaded += info =>
            {
                var a = info.animator;
                foreach (var bone in UpperBodyBones)
                {
                    var t = a.GetBoneTransform(bone);
                    if (t != null)
                    {
                        _upperBodyBones[bone.ToString()] = t;
                    }
                }
                foreach (var bone in ArmBones)
                {
                    var t = a.GetBoneTransform(bone);
                    if (t != null)
                    {
                        _armBones[bone.ToString()] = t;
                    }
                }
                
                _hasModel = true;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _upperBodyBones.Clear();
                _armBones.Clear();
            };
        }

        private void LateUpdate()
        {
            //設定がオフ -> 何もしない
            if (!_hasModel || !_boneTransferEnabled)
            {
                return;
            }

            //NOTE: HeadとHandが同一ソースの場合、結果的に単一ソースからボーン回転が読み出される
            if (_vmcpHead.IsConnected.Value)
            {
                SetBoneRotations(_vmcpHead.Humanoid, _upperBodyBones);
            }

            if (_vmcpHand.IsConnected.Value)
            {
                SetBoneRotations(_vmcpHand.Humanoid, _armBones);
            }
        }

        private void SetBoneRotations(VMCPBasedHumanoid source, Dictionary<string, Transform> dest)
        {
            foreach (var pair in dest)
            {
                pair.Value.localRotation = source.GetLocalRotation(pair.Key);
            }
        }
    }
}