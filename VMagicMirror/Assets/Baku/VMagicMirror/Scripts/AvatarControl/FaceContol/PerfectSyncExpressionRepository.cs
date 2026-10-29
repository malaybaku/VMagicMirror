using System;
using System.Collections.Generic;
using System.Linq;
using Baku.VMagicMirror.ExternalTracker;
using UnityEngine;
using UniVRM10;
using Zenject;
using Object = UnityEngine.Object;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// Owns runtime-only Perfect Sync supplements. Original expressions remain the source for
    /// user-selectable names; effective expressions are used for tracking and completeness checks.
    /// Both aliases and mesh-based supplements follow the same rebuild/notification lifecycle.
    /// </summary>
    public class PerfectSyncExpressionRepository : IInitializable
    {
        private readonly IVRMLoadable _vrmLoadable;
        private readonly IMessageReceiver _receiver;
        private readonly List<VRM10Expression> _generated = new();
        private readonly List<(SkinnedMeshRenderer Renderer, int Index)> _meshGeneratedTargets = new();
        private Vrm10Instance _instance;
        private VRM10Object _originalVrm;
        private VRM10Object _runtimeVrm;
        private bool _caseSensitive;
        private bool _useMeshBlendShapes;

        public Vrm10Instance Instance => _instance;
        public VRM10ExpressionMap OriginalMap { get; private set; }
        public VRM10ExpressionMap EffectiveMap { get; private set; }
        public event Action ExpressionsChanged;

        public PerfectSyncExpressionRepository(IVRMLoadable vrmLoadable, IMessageReceiver receiver)
        {
            _vrmLoadable = vrmLoadable;
            _receiver = receiver;
        }

        public void Initialize()
        {
            // Finish supplements before any VrmLoaded consumers cache their expression lists.
            _vrmLoadable.PreVrmLoaded += info =>
            {
                _instance = info.Instance;
                _originalVrm = _instance.Vrm;
                OriginalMap = _originalVrm.Expression.LoadExpressionMap();
                Rebuild();
            };
            _vrmLoadable.VrmDisposing += Release;
            _receiver.AssignCommandHandler(VmmCommands.SetPerfectSyncExpressionCaseSensitive, command =>
            {
                var value = command.ToBoolean();
                if (_caseSensitive == value)
                {
                    return;
                }
                _caseSensitive = value;
                if (_instance != null)
                {
                    Rebuild();
                }
            });
            _receiver.AssignCommandHandler(VmmCommands.SetPerfectSyncUseMeshBlendShapes, command =>
            {
                var value = command.ToBoolean();
                if (_useMeshBlendShapes == value)
                {
                    return;
                }
                _useMeshBlendShapes = value;
                if (_instance != null)
                {
                    Rebuild();
                }
            });
        }

        private void Rebuild()
        {
            var aliases = !_caseSensitive
                ? PerfectSyncExpressionNames.ResolveAliases(
                    OriginalMap.Keys.Where(k => k.Preset == ExpressionPreset.custom).Select(k => k.Name),
                    ExternalTrackerPerfectSync.Keys.BlendShapeNames).ToArray()
                : Array.Empty<(string Name, string SourceName)>();

            var availableNames = OriginalMap.Keys.Where(k => k.Preset == ExpressionPreset.custom)
                .Select(k => k.Name).Concat(aliases.Select(a => a.Name)).ToArray();
            var meshBindings = _useMeshBlendShapes
                ? FindMeshBindings(availableNames)
                : new Dictionary<string, List<MorphTargetBinding>>();

            // Do not touch a shared/imported VRM asset. Keep original clips themselves unchanged.
            if (_runtimeVrm == null && (aliases.Length > 0 || meshBindings.Count > 0))
            {
                _runtimeVrm = Object.Instantiate(_originalVrm);
                _instance.Vrm = _runtimeVrm;
            }

            if (_runtimeVrm != null)
            {
                // Reload preserves input weights, so clear the old inputs before removing aliases.
                var runtime = _instance.Runtime.Expression;
                foreach (var key in runtime.ExpressionKeys)
                {
                    runtime.SetWeight(key, 0f);
                }
                RemoveGenerated();
                foreach (var (name, sourceName) in aliases)
                {
                    var clip = Object.Instantiate(OriginalMap[ExpressionKey.CreateCustom(sourceName)]);
                    clip.name = name;
                    _generated.Add(clip);
                    _runtimeVrm.Expression.AddClip(ExpressionPreset.custom, clip);
                }
                foreach (var (name, bindings) in meshBindings)
                {
                    var clip = ScriptableObject.CreateInstance<VRM10Expression>();
                    clip.name = name;
                    clip.MorphTargetBindings = bindings.ToArray();
                    _generated.Add(clip);
                    _runtimeVrm.Expression.AddClip(ExpressionPreset.custom, clip);
                    foreach (var binding in bindings)
                    {
                        var target = _instance.transform.Find(binding.RelativePath).GetComponent<SkinnedMeshRenderer>();
                        _meshGeneratedTargets.Add((target, binding.Index));
                    }
                }
                runtime.Reload(_instance);
            }

            EffectiveMap = _instance.Vrm.Expression.LoadExpressionMap();
            ExpressionsChanged?.Invoke();
        }

        private void RemoveGenerated()
        {
            // Once a mesh-only expression is removed, UniVRM no longer writes to its targets.
            // Clear them explicitly so turning the option off cannot leave the last tracked pose stuck.
            foreach (var (renderer, index) in _meshGeneratedTargets)
            {
                if (renderer != null)
                {
                    renderer.SetBlendShapeWeight(index, 0f);
                }
            }
            _meshGeneratedTargets.Clear();
            foreach (var clip in _generated)
            {
                _runtimeVrm.Expression.CustomClips.Remove(clip);
                Object.Destroy(clip);
            }
            _generated.Clear();
        }

        private Dictionary<string, List<MorphTargetBinding>> FindMeshBindings(IEnumerable<string> availableNames)
        {
            var result = new Dictionary<string, List<MorphTargetBinding>>(StringComparer.Ordinal);
            foreach (var renderer in _instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }
                var names = Enumerable.Range(0, mesh.blendShapeCount).Select(mesh.GetBlendShapeName).ToArray();
                var path = UniGLTF.UnityExtensions.RelativePathFrom(renderer.transform, _instance.transform);
                // Bindings resolve by path. Skip ambiguous/unaddressable renderer paths.
                if (_instance.transform.Find(path)?.GetComponent<SkinnedMeshRenderer>() != renderer)
                {
                    continue;
                }
                foreach (var (name, index) in PerfectSyncExpressionNames.ResolveMeshShapes(
                    names, availableNames, ExternalTrackerPerfectSync.Keys.BlendShapeNames))
                {
                    if (!result.TryGetValue(name, out var bindings))
                    {
                        bindings = new List<MorphTargetBinding>();
                        result.Add(name, bindings);
                    }
                    // VRM 1.0 weights are 0..1, even for models imported from VRM 0.x.
                    bindings.Add(new MorphTargetBinding(path, index, 1f));
                }
            }
            return result;
        }

        private void Release()
        {
            if (_runtimeVrm != null)
            {
                RemoveGenerated();
                _instance.Vrm = _originalVrm;
                Object.Destroy(_runtimeVrm);
            }
            _runtimeVrm = null;
            _originalVrm = null;
            _instance = null;
            OriginalMap = null;
            EffectiveMap = null;
        }
    }
}
