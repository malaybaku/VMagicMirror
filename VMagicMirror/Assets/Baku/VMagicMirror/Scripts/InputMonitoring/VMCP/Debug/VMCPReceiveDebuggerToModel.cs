using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror.VMCP
{
    /// <summary>
    /// <see cref="VMCPReceiveDebugger"/>から呼び出すことでモデルにIKとかFKを利かすためのやつ
    /// モデル1体に1つ割り当てる
    /// </summary>
    public class VMCPReceiveDebuggerToModel: MonoBehaviour
    {
        private static readonly int FingerIndexStart = (int)HumanBodyBones.LeftThumbProximal;
        private static readonly int FingerIndexEnd = (int)HumanBodyBones.RightLittleDistal + 1;

        //NOTE: VRM 1.0で仮想ボーンの初期化が終わってるAnimatorである…というのが前提
        [SerializeField] private Animator animator;
        [SerializeField] private Vector3 posOffset;
        [SerializeField] private Transform headIk;
        [SerializeField] private Transform leftHandIk;
        [SerializeField] private Transform rightHandIk;

        private readonly Dictionary<int, Transform> _bones = new Dictionary<int, Transform>();
        private Transform _head = null;
        private Quaternion _latestHeadLocalRotation = Quaternion.identity;

        private void Start()
        {
            for (var i = FingerIndexStart; i < FingerIndexEnd; i++)
            {
                var bone = animator.GetBoneTransform((HumanBodyBones)i);
                if (bone != null)
                {
                    _bones[i] = bone;
                }
            }
            _head = animator.GetBoneTransform(HumanBodyBones.Head); 
        }

        private void LateUpdate()
        {
            //NOTE: LookAtやFBBIKの計算結果に後勝ちさせる
            _head.localRotation = _latestHeadLocalRotation;
        }
        
        public void SetFingerFK(VMCPBasedHumanoid humanoid)
        {
            for (var i = FingerIndexStart; i < FingerIndexEnd; i++)
            {
                if (_bones.TryGetValue(i, out var bone))
                {
                    bone.localRotation = humanoid.GetLocalRotation(((HumanBodyBones)i).ToString());
                }
            }
        }

        //LookAtなことに注意
        public void SetHeadIK(Pose pose)
        {
            _latestHeadLocalRotation = pose.rotation;
            headIk.SetPositionAndRotation(
                pose.position + pose.rotation * (Vector3.forward  * 2f) + posOffset, 
                pose.rotation
            );
        }

        public void SetLefHandIK(Pose pose)
            => leftHandIk.SetPositionAndRotation(pose.position + posOffset, pose.rotation);
        public void SetRightHandIK(Pose pose)
            => rightHandIk.SetPositionAndRotation(pose.position + posOffset, pose.rotation);
    }
}
