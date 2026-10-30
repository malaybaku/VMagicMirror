using System;
using System.IO;
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
        /// <summary>
        /// Returns the VRM 1.0 representation used by the normal loader, or the original bytes for VRM 1.0.
        /// Material override bindings must be validated against this layout: VRM 0.x migration can append
        /// spring bone tail nodes while preserving the original mesh and material slot indices.
        /// </summary>
        public static byte[] GetVrm10BytesForMaterialOverride(byte[] bytes)
        {
            using var parsed = new GlbLowLevelParser("", bytes).Parse();
            if (Vrm10Data.Parse(parsed) != null) return bytes;

            using var migrated = Vrm10Data.Migrate(parsed, out var vrm, out var migration);
            if (migrated == null || vrm == null)
                throw new InvalidDataException(migration?.Message ?? "Failed to migrate embedded VRM 0.x.");

            // Keep the migrated chunks before their owning GltfData is disposed. Do not modify the bundle asset.
            return new Glb(migrated.Chunks[0], migrated.Chunks[1]).ToBytes();
        }

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
