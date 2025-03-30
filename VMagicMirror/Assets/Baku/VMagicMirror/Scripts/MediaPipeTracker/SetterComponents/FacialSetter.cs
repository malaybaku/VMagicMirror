using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRM;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    //TODO: 「BlinkSource、LipSyncSource、PerfectSync用の何か」らへんとして動作するようにI/F実装として整理していく
    
    /// <summary>
    /// <see cref="FaceResultSetter"/> 等に向けて、Mediapipe Face Landmarkerで取得した表情の情報を集約するやつ
    /// 
    /// </summary>
    public class FacialSetter : MonoBehaviour
    {
        [SerializeField] private VRMBlendShapeProxy blendShapeProxy;

        // NOTE: Play Mode中に増えたキーは減らない…という前提を置く
        private readonly Dictionary<BlendShapeKey, float> _values = new();
        private readonly List<BlendShapeKey> _valuesKeyCache = new();
        private readonly object _dictLock = new();

        private readonly HashSet<BlendShapeKey> _availableCustomKeys = new();

        public void SetValues(IEnumerable<KeyValuePair<string, float>> values)
        {
            lock (_dictLock)
            {
                foreach (var (keyName, value) in values)
                {
                    // NOTE: 大文字/小文字をケアしてます
                    if (TryGetTargetKey(keyName, out var key))
                    {
                        _values[key] = value;
                    }
                }
            }
        }

        public void ResetValues()
        {
            lock (_dictLock)
            {
                _valuesKeyCache.Clear();
                _valuesKeyCache.AddRange(_values.Keys);
                foreach (var key in _valuesKeyCache)
                {
                    _values[key] = 0f;
                }
            }
        }

        private bool TryGetTargetKey(string keyName, out BlendShapeKey result)
        {
            if (_availableCustomKeys.Any(
                    k => k.Name.Equals(keyName, StringComparison.InvariantCultureIgnoreCase)
                    ))
            {
                result = _availableCustomKeys.First(
                    k => k.Name.Equals(keyName, StringComparison.InvariantCultureIgnoreCase)
                );
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        private void Start()
        {
            _availableCustomKeys.UnionWith(blendShapeProxy.BlendShapeAvatar.Clips
                .Select(c => c.Key)
                .Where(key => key.Preset is BlendShapePreset.Unknown)
            );
        }

        private void Update()
        {
            lock (_dictLock)
            {
                blendShapeProxy.SetValues(_values);
            }
        }
    }
}