using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
    public class MediaPipeFacialValueRepository : PresenterBase, ITickable
    {
        private readonly IVRMLoadable _vrmLoadable;
        private readonly MediapipePoseSetterSettings _settings;
        private readonly object _dataLock = new();

        private readonly CounterBoolState _isTracked = new(3, 5);
        private readonly HashSet<ExpressionKey> _availableCustomKeys = new();
        private readonly Dictionary<ExpressionKey, float> _values = new();
        private readonly HashSet<ExpressionKey> _valuesKeyCache = new();
        private bool _hasModel;
        private float _trackLostTime;
        
        [Inject]
        public MediaPipeFacialValueRepository(
            IVRMLoadable vrmLoadable,
            MediapipePoseSetterSettings settings)
        {
            _vrmLoadable = vrmLoadable;
            _settings = settings;
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

        public bool IsTracked
        {
            get
            {
                lock (_dataLock)
                {
                    return _isTracked.Value;
                }
            }
        }
        
        //TODO: case sensitiveの対策検討？
        // MediaPipeが言ってくるほうはケース固定だから大丈夫じゃない？まあ何にせよ注意は必要だし文面化はしたい
        /// <summary>
        /// NOTE: typoの防止策としては、必要なキーを <see cref="Baku.VMagicMirror.ExternalTracker.ExternalTrackerPerfectSync.Keys"/> を参照して取得するのが良い…のか…？
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public float GetValue(ExpressionKey key) => _values.GetValueOrDefault(key, 0f);

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
                
                _isTracked.Set(true);
                _trackLostTime = 0f;
            }
        }

        public void RequestReset()
        {
            lock (_dataLock)
            {
                _isTracked.Set(false);
                _trackLostTime = 0f;
                if (!_isTracked.Value)
                {
                    ResetFacialValues();
                }
            }
        }

        private void ResetFacialValues()
        {
            // NOTE: MediaPipeのブレンドシェイプ数は特に変動しない…という前提の実装
            _valuesKeyCache.UnionWith(_values.Keys);
            foreach (var key in _valuesKeyCache)
            {
                _values[key] = 0f;
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

        void ITickable.Tick()
        {
            lock (_dataLock)
            {
                if (!_isTracked.Value)
                {
                    return;
                }

                _trackLostTime += Time.deltaTime;
                if (_trackLostTime >= _settings.TrackingLostWaitDuration)
                {
                    _isTracked.Reset(false);
                    _trackLostTime = 0f;
                    ResetFacialValues();
                }
            }
        }
    }
}