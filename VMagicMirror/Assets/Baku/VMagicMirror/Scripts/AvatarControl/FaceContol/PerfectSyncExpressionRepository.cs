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
    /// Mesh-based supplements can be added to Rebuild without changing that distinction.
    /// </summary>
    public class PerfectSyncExpressionRepository : IInitializable
    {
        private readonly IVRMLoadable _vrmLoadable;
        private readonly IMessageReceiver _receiver;
        private readonly List<VRM10Expression> _generated = new();
        private Vrm10Instance _instance;
        private VRM10Object _originalVrm;
        private VRM10Object _runtimeVrm;
        private bool _ignoreCase;

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
            _receiver.AssignCommandHandler(VmmCommands.SetPerfectSyncIgnoreCase, command =>
            {
                var value = command.ToBoolean();
                if (_ignoreCase == value)
                {
                    return;
                }
                _ignoreCase = value;
                if (_instance != null)
                {
                    Rebuild();
                }
            });
        }

        private void Rebuild()
        {
            var aliases = _ignoreCase
                ? PerfectSyncExpressionNames.ResolveAliases(
                    OriginalMap.Keys.Where(k => k.Preset == ExpressionPreset.custom).Select(k => k.Name),
                    ExternalTrackerPerfectSync.Keys.BlendShapeNames).ToArray()
                : Array.Empty<(string Name, string SourceName)>();

            // Do not touch a shared/imported VRM asset. Keep original clips themselves unchanged.
            if (_runtimeVrm == null && aliases.Length > 0)
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
                runtime.Reload(_instance);
            }

            EffectiveMap = _instance.Vrm.Expression.LoadExpressionMap();
            ExpressionsChanged?.Invoke();
        }

        private void RemoveGenerated()
        {
            foreach (var clip in _generated)
            {
                _runtimeVrm.Expression.CustomClips.Remove(clip);
                Object.Destroy(clip);
            }
            _generated.Clear();
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
