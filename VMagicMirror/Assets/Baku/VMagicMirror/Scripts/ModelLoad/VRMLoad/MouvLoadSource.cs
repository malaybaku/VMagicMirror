using System;
using System.IO;
using Baku.MaterialOverridableVrm;
using UniGLTF;
using UniVRM10;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// Keeps loaded assets alive until the model's override materials are released.
    /// The bundle itself is detached immediately so the same file can be overwritten and reloaded.
    /// Use the normal VMM VRM importer so metadata and non-overridden materials behave as usual.
    /// Embedded VRM 0.x is migrated by that importer; validate overrides against the migrated layout.
    /// Do not use the package's MaterialOverrideImporter here, which has a separate VRM loading path.
    /// All calls must run on Unity's main thread.
    /// </summary>
    internal sealed class MouvLoadSource : IDisposable
    {
        private MaterialOverrideLease _lease;
        private bool _disposed;
        public MaterialOverrideData Data { get; private set; }

        public MouvLoadSource(string path)
        {
            var fullPath = Path.GetFullPath(path);
            var bundle = AssetBundle.LoadFromFile(fullPath);
            if (!bundle) throw new IOException($"Could not load MOUV AssetBundle: {fullPath}");
            var loaded = false;
            try
            {
                var assets = bundle.LoadAllAssets<MaterialOverrideData>();
                if (assets.Length != 1) throw new InvalidDataException("Expected exactly one MaterialOverrideData asset.");
                Data = assets[0];
                MaterialOverrideApplicator.Validate(Data);
                loaded = true;
            }
            finally
            {
                // Keep this load's data and its material/texture/shader dependencies, but release
                // the bundle identity and file handle before any asynchronous VRM import starts.
                // A subsequent preview or load must read the file again, even at the same path.
                // On failure no model owns these assets, so unload them together with the bundle.
                bundle.Unload(!loaded);
            }
        }

        public void Apply(Vrm10Instance instance)
        {
            var bytes = UniVrmLoader.GetVrm10BytesForMaterialOverride(Data.VrmBytes);
            if (ReferenceEquals(bytes, Data.VrmBytes))
            {
                _lease = MaterialOverrideApplicator.Apply(instance.GetComponent<RuntimeGltfInstance>(), Data);
                instance.GetComponent<RuntimeGltfInstance>().ShowMeshes();
                return;
            }

            // Migration adds spring bone tail nodes, so applying against the original VRM 0.x bytes
            // would fail the package's node-count check. Use a temporary data object rather than
            // changing the shared bundle asset. The normal load still uses the original bytes and metadata.
            var migratedData = ScriptableObject.CreateInstance<MaterialOverrideData>();
            try
            {
                migratedData.FormatVersion = Data.FormatVersion;
                migratedData.VrmBytes = bytes;
                migratedData.Bindings = Data.Bindings;
                _lease = MaterialOverrideApplicator.Apply(instance.GetComponent<RuntimeGltfInstance>(), migratedData);
                instance.GetComponent<RuntimeGltfInstance>().ShowMeshes();
            }
            finally
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(migratedData);
                else UnityEngine.Object.DestroyImmediate(migratedData);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_lease) _lease.Dispose();
            _lease = null;
            var data = Data;
            Data = null;
            if (data)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(data);
                else UnityEngine.Object.DestroyImmediate(data);
            }
            // Unload(false) detached the assets from their bundle. Reclaim unused dependencies
            // through Unity's reference tracking so assets still used by another model survive.
            Resources.UnloadUnusedAssets();
        }
    }
}
