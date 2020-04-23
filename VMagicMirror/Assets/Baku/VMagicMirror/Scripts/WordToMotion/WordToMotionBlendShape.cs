using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRM;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// Word To Motionのブレンドシェイプを適用する。
    /// </summary>
    /// <remarks>
    /// このクラスは実行タイミングが遅く、有効時には他の表情制御をほぼ完全にオーバーライドする。
    /// </remarks>
    public class WordToMotionBlendShape : MonoBehaviour
    {
        private VRMBlendShapeProxy _proxy = null;
        private BlendShapeKey[] _allBlendShapeKeys = new BlendShapeKey[0];

        private readonly Dictionary<BlendShapeKey, float> _blendShape = new Dictionary<BlendShapeKey, float>();

        private bool _reserveBlendShapeReset = false;
        
        public void Initialize(VRMBlendShapeProxy proxy)
        {
            _proxy = proxy;
            _allBlendShapeKeys = _proxy
                .BlendShapeAvatar
                .Clips
                .Select(c => BlendShapeKeyFactory.CreateFrom(c.BlendShapeName))
                .ToArray();
        }

        public void DisposeProxy()
        {
            _proxy = null;
            _allBlendShapeKeys = new BlendShapeKey[0];
        }

        /// <summary>
        /// Word To Motionによるブレンドシェイプを指定します。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <remarks>1つ以上のブレンドシェイプを指定すると通常の表情制御をオーバーライドする。</remarks>
        public void Add(BlendShapeKey key, float value)
        {
            if (_allBlendShapeKeys.Any(k => k.Name == key.Name))
            {
                _blendShape[key] = value;
            }
        }

        /// <summary>Word To Motionによる表情制御を無効化(終了)します。</summary>
        public void Clear()
        {
            _blendShape.Clear();
        }

        public void ResetBlendShape()
        {
            if (_blendShape.Count > 0)
            {
                _reserveBlendShapeReset = true;
                Clear();
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
                //オーバーライド不要なケース
                _proxy?.Apply();
                return;
            }

            //一回ApplyすることでAccumulateした値を捨てさせる
            _proxy.Apply();
            
            //実際に適用したい値を入れなおして再度Applyすると完全上書きになる
            for(int i = 0; i < _allBlendShapeKeys.Length; i++)
            {
                if (_blendShape.TryGetValue(_allBlendShapeKeys[i], out float value))
                {
                    _proxy.AccumulateValue(_allBlendShapeKeys[i], value);
                }
                else
                {
                    //完全な排他制御をしたいので、指定がない値は明示的にゼロ書き込みする
                    _proxy.AccumulateValue(_allBlendShapeKeys[i], 0);
                }
            }
            _proxy.Apply();
        }

    }
}
