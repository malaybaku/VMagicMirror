using System;
using System.Collections.Generic;
using System.Linq;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    //TODO: 下記のようなことをやって「BlinkSource、LipSyncSource、PerfectSync用の何か」らへんとして動作するようにI/Fを整える
    // - 他のコードとノリを合わせてIVRMLoadableを見に行く
    // - 必要ならblinkerとかlipsyncのサブクラスを持って、そっちに値を流す
    
    /// <summary>
    /// <see cref="FaceResultSetter"/> 等に向けて、Mediapipe Face Landmarkerで取得した表情の情報を集約するやつ
    /// </summary>
    public class FacialSetter : PresenterBase
    {
        private readonly IVRMLoadable _vrmLoadable;
        private readonly object _dataLock = new();

        private readonly HashSet<ExpressionKey> _availableCustomKeys = new();
        private readonly Dictionary<ExpressionKey, float> _values = new();
        private readonly List<ExpressionKey> _valuesKeyCache = new();
        private bool _hasModel;
        
        [Inject]
        public FacialSetter(IVRMLoadable vrmLoadable)
        {
            _vrmLoadable = vrmLoadable;
        }
        
        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnModelLoaded;
            _vrmLoadable.VrmDisposing += OnModelUnloaded;
        }

        private void OnModelUnloaded()
        {
            _hasModel = false;
            _availableCustomKeys.Clear();
        }

        private void OnModelLoaded(VrmLoadedInfo info)
        {
            lock (_dataLock)
            {
                _availableCustomKeys.Clear();
                _availableCustomKeys.UnionWith(info.RuntimeFacialExpression
                    .ExpressionKeys
                    .Where(key => key.Preset is ExpressionPreset.custom)
                );
                _hasModel = true;
            }
        }

        public void SetValues(IEnumerable<KeyValuePair<string, float>> values)
        {
            lock (_dataLock)
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
            lock (_dataLock)
            {
                _valuesKeyCache.Clear();
                _valuesKeyCache.AddRange(_values.Keys);
                foreach (var key in _valuesKeyCache)
                {
                    _values[key] = 0f;
                }
            }
        }

        private bool TryGetTargetKey(string keyName, out ExpressionKey result)
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

            result = default;
            return false;
        }
    }
}