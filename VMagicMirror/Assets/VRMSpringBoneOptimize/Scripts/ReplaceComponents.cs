namespace VRM.Optimize
{
    using UnityEngine;
    using VRMSpringBone = VRM.VRMSpringBone;
#if ENABLE_JOB_SPRING_BONE
    using JobVRMSpringBone = VRM.Optimize.Jobs.VRMSpringBoneJob;
    using JobVRMSpringBoneColliderGroup = VRM.Optimize.Jobs.VRMSpringBoneColliderGroupJob;
#endif

    // VRM.VRMSpringBoneがUniVRMのバージョンによってはasmdef非対応なので、
    // asmdef非依存で参照を持ってこれるように敢えて配下には含めない.

    public static class ReplaceComponents
    {
#if ENABLE_JOB_SPRING_BONE
        public static void ReplaceJobs(GameObject model, bool isEditor = false)
        {
            var springBones = model.GetComponentsInChildren<VRMSpringBone>(includeInactive:true);
            foreach (var oldComponent in springBones)
            {
                // JobSystem版のComponentをつけた上で、
                // 置き換え前のComponentからシリアライズされているデータをコピー
                var newComponent = oldComponent.gameObject.AddComponent<JobVRMSpringBone>();
                newComponent.m_stiffnessForce = oldComponent.m_stiffnessForce;
                newComponent.m_gravityPower = oldComponent.m_gravityPower;
                newComponent.m_gravityDir = oldComponent.m_gravityDir;
                newComponent.m_dragForce = oldComponent.m_dragForce;
                newComponent.m_center = oldComponent.m_center;
                newComponent.RootBones = oldComponent.RootBones;
                newComponent.m_hitRadius = oldComponent.m_hitRadius;

                // VRMSpringBoneColliderGroupの情報をコピー
                var oldColliders = oldComponent.ColliderGroups;
                if (oldColliders == null || oldColliders.Length <= 0) continue;
                var newColliders = new JobVRMSpringBoneColliderGroup[oldColliders.Length];
                for (var j = 0; j < oldColliders.Length; j++)
                {
                    var oldCollider = oldColliders[j];
                    var newCollider = oldCollider.gameObject.GetComponent<JobVRMSpringBoneColliderGroup>();
                    if (newCollider == null)
                    {
                        newCollider = oldCollider.gameObject.AddComponent<JobVRMSpringBoneColliderGroup>();
                        var oldSphereColliders = oldCollider.Colliders;
                        var newSphereColliders =
                            new JobVRMSpringBoneColliderGroup.SphereCollider[oldSphereColliders.Length];
                        for (var k = 0; k < oldSphereColliders.Length; k++)
                        {
                            newSphereColliders[k] = new JobVRMSpringBoneColliderGroup.SphereCollider
                            {
                                Offset = oldSphereColliders[k].Offset,
                                Radius = oldSphereColliders[k].Radius,
                            };
                        }

                        newCollider.Colliders = newSphereColliders;
                    }

                    newColliders[j] = newCollider;
                }

                newComponent.ColliderGroups = newColliders;
            }

#if UNITY_EDITOR
            if (isEditor)
            {
                DestroyComponentsEditor(springBones);
            }
            else
#endif
            {
                DestroyComponents(springBones);
            }
        }
#endif

        static void DestroyComponents(VRMSpringBone[] springBones)
        {
            foreach (var oldComponent in springBones)
            {
                var oldColliders = oldComponent.ColliderGroups;
                if (oldColliders != null)
                {
                    foreach (var t in oldColliders)
                    {
                        GameObject.Destroy(t);
                    }
                }

                GameObject.Destroy(oldComponent);
            }
        }

#if UNITY_EDITOR
        static void DestroyComponentsEditor(VRMSpringBone[] springBones)
        {
            foreach (var oldComponent in springBones)
            {
                var oldColliders = oldComponent.ColliderGroups;
                if (oldColliders != null)
                {
                    foreach (var t in oldColliders)
                    {
                        GameObject.DestroyImmediate(t);
                    }
                }

                GameObject.DestroyImmediate(oldComponent);
            }
        }
#endif
    }
}
