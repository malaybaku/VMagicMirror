using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VeryAnimation;
using Zenject;

namespace Baku.VMagicMirror.VMCP
{
    //NOTE: このクラスの`Set`はIKの適用より後、かつVRM1.0のRuntimeのProcess()よりは先に呼ばれる必要がある。のでMonoBehaviour。
    public class VMCPNaiveBoneTransfer : MonoBehaviour
    {
        //NOTE:
        // - このクラスの守備範囲はFinger以外のボーン全般
        //   - Headを送ってるSourceがSpine~Headのボーン回転を指定できる
        //   - Handを送ってるSourceがShoulder~Handのボーン回転を指定できる
        // - - LowerBodyを送ってるSourceはHips~Toesのボーン回転を指定できる
        // - 指ボーンは取得できている場合、VMCPHandに基づいてVMCPBasedFingerSetterが処理するので、ここでの処理は不要
        private static readonly HumanBodyBones[] UpperBodyBones = new[]
        {
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.UpperChest,
            HumanBodyBones.Neck,
            HumanBodyBones.Head,
        };

        private static readonly HumanBodyBones[] LowerBodyBones = new[]
        {
            HumanBodyBones.Hips,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.LeftToes,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.RightFoot,
            HumanBodyBones.RightToes,
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

        // 腕以外でSpine ~ Head
        private readonly Dictionary<string, Transform> _upperBodyBones = new();

        // Hips ~ Toesまでの両脚ぶん
        private readonly Dictionary<string, Transform> _lowerBodyBones = new();

        // Shoulder~Handまでの両腕ぶん
        private readonly Dictionary<string, Transform> _armBones = new();

        
        private bool _hasModel;
        private bool _boneTransferEnabled;

        private IMessageReceiver _receiver;
        private IVRMLoadable _vrmLoadable;
        private VMCPHandPose _vmcpHand;
        private VMCPHeadPose _vmcpHead;
        private VMCPLowerBodyPose _vmcpLowerBodyPose;

        //NOTE: WordToMotionで上半身を動かすときweightを下げる
        private CancellationTokenSource _weightCts;
        private float _applyWeight = 1f;

        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            IVRMLoadable vrmLoadable,
            VMCPHandPose vmcpHand,
            VMCPHeadPose vmcpHead,
            VMCPLowerBodyPose vmcpLowerBodyPose)
        {
            _receiver = receiver;
            _vrmLoadable = vrmLoadable;
            _vmcpHand = vmcpHand;
            _vmcpHead = vmcpHead;
            _vmcpLowerBodyPose = vmcpLowerBodyPose;

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

                foreach (var bone in LowerBodyBones)
                {
                    var t = a.GetBoneTransform(bone);
                    if (t != null)
                    {
                        _lowerBodyBones[bone.ToString()] = t;
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
                _lowerBodyBones.Clear();
                _armBones.Clear();
            };
        }

        public void FadeInWeight(float duration) => FadeWeight(1f, duration);
        public void FadeOutWeight(float duration) => FadeWeight(0f, duration);

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

            if (_vmcpLowerBodyPose.IsConnected.Value)
            {
                var lowerBodyHumanoid = _vmcpLowerBodyPose.Humanoid;
                SetBoneRotations(lowerBodyHumanoid, _lowerBodyBones);
                // 本アバターのRootBone自体は動かさず、Hipsを目標位置に持っていく
                if (lowerBodyHumanoid.RootPose is { } rootPose && 
                    lowerBodyHumanoid.HipsLocalPosition is { } hipsPosition)
                {
                    var hipsWorldPosition = rootPose.position + rootPose.rotation * hipsPosition;
                    var hipsWorldRotation = 
                        rootPose.rotation * _vmcpHand.Humanoid.GetLocalRotation(nameof(HumanBodyBones.Hips));
                    _lowerBodyBones[nameof(HumanBodyBones.Hips)].SetPositionAndRotation(
                        hipsWorldPosition, hipsWorldRotation
                    );
                }
            }
        }

        private void OnDestroy()
        {
            _weightCts?.Cancel();
            _weightCts?.Dispose();
        }

        //NOTE: 同一ボーンに対して毎フレーム最大1回しか呼ばない…という前提で実装されてる (補間の部分が)
        private void SetBoneRotations(VMCPBasedHumanoid source, Dictionary<string, Transform> dest)
        {
            if (_applyWeight <= 0f)
            {
                return;
            }

            if (_applyWeight >= 1f)
            {
                foreach (var pair in dest)
                {
                    pair.Value.localRotation = source.GetLocalRotation(pair.Key);
                }
                return;
            }
            
            //補間が必要なケースだけ補間する
            foreach (var pair in dest)
            {
                pair.Value.localRotation = Quaternion.Slerp(
                    pair.Value.localRotation,
                    source.GetLocalRotation(pair.Key),
                    _applyWeight
                );
            }
        }

        private void FadeWeight(float target, float duration)
        {
            _weightCts?.Cancel();
            _weightCts?.Dispose();
            _weightCts = new();
            FadeWeightAsync(target, duration, _weightCts.Token).Forget();
        }

        private async UniTaskVoid FadeWeightAsync(float target, float duration, CancellationToken ct)
        {
            if (duration <= 0f)
            {
                _applyWeight = target;
                return;
            }

            if (target > .5f)
            {
                while (_applyWeight < 1f)
                {
                    _applyWeight = Mathf.Clamp01(_applyWeight + Time.deltaTime / duration);
                    await UniTask.NextFrame(ct);
                }
            }
            else
            {
                while (_applyWeight > 0f)
                {
                    _applyWeight = Mathf.Clamp01(_applyWeight - Time.deltaTime / duration);
                    await UniTask.NextFrame(ct);
                }
            }
        }
    }
}