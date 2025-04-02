using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    /// <summary>
    /// Mediapipe Face Landmarkerで取得した表情の情報を保持するクラス。データが古くなると自動で捨てる実装も入っている
    /// </summary>
    public class MediaPipeFacialValueRepository : ITickable
    {
        private readonly MediapipePoseSetterSettings _settings;
        private readonly object _dataLock = new();

        private readonly CounterBoolState _isTracked = new(3, 5);
        private readonly Dictionary<string, float> _values = new();
        private float _trackLostTime;
        
        [Inject]
        public MediaPipeFacialValueRepository(MediapipePoseSetterSettings settings)
        {
            _settings = settings;
            foreach(var key in MediaPipeBlendShapeKeys.Keys)
            {
                _values[key] = 0f;
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
        
        /// <summary>
        /// NOTE: <see cref="MediaPipeBlendShapeKeys"/> で定義されたキーを指定すること。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public float GetValue(string key) => _values.GetValueOrDefault(key, 0f);

        public void SetValues(IEnumerable<KeyValuePair<string, float>> values)
        {
            lock (_dataLock)
            {
                // NOTE: 前提として (valuesのキー一覧) == (_valuesのキー一覧) であることを期待してるが、
                // パフォーマンス的にそのチェックをしたくないので省く
                foreach (var (keyName, value) in values)
                {
                    _values[keyName] = value;
                }
                
                _isTracked.Set(true);
                _trackLostTime = 0f;
            }
        }

        /// <summary>
        /// NOTE: 画像の解析結果として顔が検出できなかったときに呼び出す。
        /// このメソッドを続けて何度か呼び出すことで、実際にトラッキングロストしたものと見なされる。
        /// </summary>
        public void RequestReset()
        {
            lock (_dataLock)
            {
                _isTracked.Set(false);
                if (!_isTracked.Value)
                {
                    ResetFacialValues();
                }
            }
        }

        private void ResetFacialValues()
        {
            foreach (var key in MediaPipeBlendShapeKeys.Keys)
            {
                _values[key] = 0f;
            }
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