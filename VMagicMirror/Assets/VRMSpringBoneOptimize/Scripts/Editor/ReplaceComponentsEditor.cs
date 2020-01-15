namespace VRM.Optimize.Editor
{
    using UnityEngine;
    using UnityEditor;

    // VRM.VRMSpringBoneがUniVRMのバージョンによってはasmdef非対応なので、
    // asmdef非依存で参照を持ってこれるように敢えて配下には含めない.

    public static class ReplaceComponentsEditor
    {
#if ENABLE_JOB_SPRING_BONE
        [MenuItem("VRMSpringBoneOptimize/Replace SpringBone Components - Jobs")]
        static void ReplaceJobComponents()
        {
            EditorApplication.delayCall += () =>
            {
                var models = GameObject.FindObjectsOfType<VRMMeta>();
                foreach (var model in models)
                {
                    ReplaceComponents.ReplaceJobs(model.gameObject, true);
                }
            };
        }
#endif
    }
}
