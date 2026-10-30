using System;
using System.Collections.Generic;
using System.IO;
using Baku.MaterialOverridableVrm;
using UniGLTF;
using UniVRM10;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// Keeps bundle assets alive until the model's override materials are released.
    /// Use the normal VMM VRM importer so metadata and non-overridden materials behave as usual.
    /// Embedded VRM 0.x is migrated by that importer; validate overrides against the migrated layout.
    /// Do not use the package's MaterialOverrideImporter here, which has a separate VRM loading path.
    /// All calls must run on Unity's main thread.
    /// </summary>
    internal sealed class MouvLoadSource : IDisposable
    {
        private sealed class Entry
        {
            public AssetBundle Bundle;
            public int Users;
        }

        private static readonly Dictionary<string, Entry> Bundles = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _path;
        private readonly Entry _entry;
        private MaterialOverrideLease _lease;
        private bool _disposed;
        public MaterialOverrideData Data { get; }

        public MouvLoadSource(string path)
        {
            _path = Path.GetFullPath(path);
            if (!Bundles.TryGetValue(_path, out _entry))
            {
                var bundle = AssetBundle.LoadFromFile(_path);
                if (!bundle) throw new IOException($"Could not load MOUV AssetBundle: {_path}");
                _entry = new Entry { Bundle = bundle };
                Bundles.Add(_path, _entry);
            }
            _entry.Users++;
            try
            {
                var assets = _entry.Bundle.LoadAllAssets<MaterialOverrideData>();
                if (assets.Length != 1) throw new InvalidDataException("Expected exactly one MaterialOverrideData asset.");
                Data = assets[0];
                MaterialOverrideApplicator.Validate(Data);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Apply(Vrm10Instance instance)
        {
            var bytes = UniVrmLoader.GetVrm10BytesForMaterialOverride(Data.VrmBytes);
            if (ReferenceEquals(bytes, Data.VrmBytes))
            {
                _lease = MaterialOverrideApplicator.Apply(instance.GetComponent<RuntimeGltfInstance>(), Data);
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
            if (--_entry.Users != 0) return;
            Bundles.Remove(_path);
            _entry.Bundle.Unload(true);
        }
    }
}
