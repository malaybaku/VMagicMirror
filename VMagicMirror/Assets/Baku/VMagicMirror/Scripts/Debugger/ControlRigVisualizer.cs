using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniVRM10;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// VRM 1.0モデルがロードされたシーンにこのコンポーネントを含むprefabを置くことで、ControlRigがビジュアライズされます
    /// </summary>
    public class ControlRigVisualizer : MonoBehaviour
    {
        [SerializeField] private ControlRigVisualizerBone bonePrefab;

        private void Start()
        {
            var instance = FindObjectOfType<Vrm10Instance>();
            if (instance == null)
            {
                Debug.LogWarning("There is no VRM10Instance, could not initialize visualizer");
                return;
            }

            if (instance.Runtime.ControlRig == null)
            {
                Debug.LogError("ControlRig does not exist in VRM instance. Check ControlRigGenerateOption settings");
                return;
            }

            Initialize(instance.Runtime.ControlRig);
        }

        private void Initialize(Vrm10RuntimeControlRig controlRig)
        {
            var bones = new HashSet<ControlRigVisualizerBone>();
            var t = transform;
            foreach (var source in controlRig.Bones)
            {
                var bone = Instantiate(bonePrefab, t);
                bone.Attach(source.Value);
                bones.Add(bone);
            }

            foreach (var bone in bones)
            {
                //NOTE: ControlRigはHumanoidBoneだけで構成されるので、アタッチしたボーンの階層からビジュアライザの階層が組める
                var parent = 
                    bones.FirstOrDefault(b => b.AttachTarget == bone.AttachTargetParent);
                if (parent != null)
                {
                    bone.SetBoneParent(parent);
                }
            }
        }
    }
}
