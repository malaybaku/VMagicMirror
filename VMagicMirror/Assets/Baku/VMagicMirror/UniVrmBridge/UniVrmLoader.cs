using System;
using System.Threading;
using System.Threading.Tasks;
using UniGLTF;
using UniGLTF.Extensions.VRMC_vrm;
using UniVRM10;
using UniVRM10.Migration;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// Keeps UniGLTF.Utils types out of APIs used by the predefined Assembly-CSharp assembly.
    /// Do not add references to VMM's main assembly or its asset dependencies here.
    /// </summary>
    public static class UniVrmLoader
    {
        public static Task<Vrm10Instance> LoadBytesAsync(
            byte[] bytes,
            bool canLoadVrm0X = true,
            ControlRigGenerationOption controlRigGenerationOption = ControlRigGenerationOption.Generate,
            bool showMeshes = true,
            IMaterialDescriptorGenerator materialGenerator = null,
            Action<Texture2D, Meta, Vrm0Meta> vrmMetaInformationCallback = null,
            CancellationToken ct = default)
        {
            return Vrm10.LoadBytesAsync(bytes, canLoadVrm0X, controlRigGenerationOption, showMeshes,
                materialGenerator: materialGenerator,
                vrmMetaInformationCallback: vrmMetaInformationCallback == null
                    ? null
                    : (thumbnail, meta, oldMeta) => vrmMetaInformationCallback(thumbnail, meta, oldMeta),
                ct: ct);
        }

        public static Task<RuntimeGltfInstance> LoadAnimationAsync(VrmAnimationImporter importer)
            => importer.LoadAsync(new ImmediateCaller());

        public static Task<RuntimeGltfInstance> LoadRuntimeAsync(ImporterContext importer)
        {
#if UNITY_WEBGL
            return importer.LoadAsync(new RuntimeOnlyNoThreadAwaitCaller());
#else
            return importer.LoadAsync(new RuntimeOnlyAwaitCaller());
#endif
        }
    }
}
