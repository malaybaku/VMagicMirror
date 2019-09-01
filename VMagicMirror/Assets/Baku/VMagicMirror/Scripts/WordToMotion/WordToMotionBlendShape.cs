using System;
using System.Collections.Generic;
using UnityEngine;
using VRM;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// Type To Motionのブレンドシェイプを適用する。
    /// </summary>
    /// <remarks>
    /// このクラスは実行タイミングが遅く、有効時には他の表情制御をほぼ完全にオーバーライドする。
    /// </remarks>
    public class WordToMotionBlendShape : MonoBehaviour
    {
        private VRMBlendShapeProxy _proxy = null;

        private BlendShapeKey[] _allBlendShapeKeys = null;

        private readonly Dictionary<BlendShapeKey, float> _blendShape = new Dictionary<BlendShapeKey, float>();

        private bool _reserveBlendShapeReset = false;

        public void Initialize(VRMBlendShapeProxy proxy)
        {
            _proxy = proxy;
        }

        public void DisposeProxy() => _proxy = null;

        /// <summary>
        /// Word To Motionによるブレンドシェイプを指定します。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <remarks>1つ以上のブレンドシェイプを指定すると通常の表情制御をオーバーライドする。</remarks>
        public void Add(BlendShapeKey key, float value)
        {
            _blendShape[key] = value;
        }

        /// <summary>Word To Motionによる表情制御を無効化(終了)します。</summary>
        public void Clear()
        {
            _blendShape.Clear();
        }

        public void ResetBlendShape()
        {
            //事前に
            if (_blendShape.Count > 0)
            {
                _reserveBlendShapeReset = true;
                Clear();
            }
        }

        private void Start()
        {
            var presets = Enum.GetValues(typeof(BlendShapePreset));
            _allBlendShapeKeys = new BlendShapeKey[presets.Length];
            for(int i = 0; i < _allBlendShapeKeys.Length; i++)
            {
                _allBlendShapeKeys[i] = new BlendShapeKey((BlendShapePreset)presets.GetValue(i));
            }
        }

        private void Update()
        {
            if (_reserveBlendShapeReset)
            {
                for (int i = 0; i < _allBlendShapeKeys.Length; i++)
                {
                    _proxy.AccumulateValue(_allBlendShapeKeys[i], 0f);
                }
                _proxy?.Apply();
                _reserveBlendShapeReset = false;
            }
        }

        private void LateUpdate()
        {
            if (_proxy == null || _blendShape.Count == 0)
            {
                //オーバーライド不要
                return;
            }

            for(int i = 0; i < _allBlendShapeKeys.Length; i++)
            {
                if (_blendShape.TryGetValue(_allBlendShapeKeys[i], out float value))
                {
                    _proxy.AccumulateValue(_allBlendShapeKeys[i], value);
                }
                else
                {
                    //ここがポイント: オーバーライドなので関係ないブレンドシェイプは叩き落す
                    _proxy.AccumulateValue(_allBlendShapeKeys[i], 0f);
                }
            }
            _proxy?.Apply();
        }

    }
}
