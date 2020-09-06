using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRM;
using Zenject;

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
        private static readonly BlendShapeKey[] _lipSyncKeys = new []
        {
            new BlendShapeKey(BlendShapePreset.A),
            new BlendShapeKey(BlendShapePreset.I),
            new BlendShapeKey(BlendShapePreset.U),
            new BlendShapeKey(BlendShapePreset.E),
            new BlendShapeKey(BlendShapePreset.O),
        };
        
        private VRMBlendShapeProxy _proxy = null;
        private BlendShapeKey[] _allBlendShapeKeys = new BlendShapeKey[0];

        private readonly Dictionary<BlendShapeKey, float> _blendShape = new Dictionary<BlendShapeKey, float>();

        private bool _reserveBlendShapeReset = false;

        private EyeBonePostProcess _eyeBoneResetter;
        
        [Inject]
        public void Initialize(EyeBonePostProcess eyeBoneResetter)
        {
            _eyeBoneResetter = eyeBoneResetter;
        }
        
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

        /// <summary> trueの場合、このスクリプトではリップシンクのブレンドシェイプに書き込みを行いません。 </summary>
        public bool SkipLipSyncKeys { get; set; }
        
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
        
        // private void Update()
        // {
        //     if (_reserveBlendShapeReset)
        //     {
        //         for (int i = 0; i < _allBlendShapeKeys.Length; i++)
        //         {
        //             var key = _allBlendShapeKeys[i];
        //             _proxy.AccumulateValue(key, 0f);                        
        //         }
        //         _proxy?.Apply();
        //         _reserveBlendShapeReset = false;
        //     }
        // }

        // private void LateUpdate()
        // {
        //     if (_proxy == null || _blendShape.Count == 0)
        //     {
        //         //オーバーライド不要なケース
        //         _proxy?.Apply();
        //         return;
        //     }
        //
        //     //一回ApplyすることでAccumulateした値を捨てさせる
        //     _proxy.Apply();
        //     
        //     //実際に適用したい値を入れなおして再度Applyすると完全上書きになる
        //     for(int i = 0; i < _allBlendShapeKeys.Length; i++)
        //     {
        //         var key = _allBlendShapeKeys[i];
        //         //リップシンクをそのままにするかどうかは場合による
        //         if (SkipLipSyncKeys && _lipSyncKeys.Contains(key))
        //         {
        //             continue;
        //         }
        //             
        //         if (_blendShape.TryGetValue(key, out float value))
        //         {
        //             _proxy.AccumulateValue(key, value);
        //         }
        //         else
        //         {
        //             //完全な排他制御をしたいので、指定がない値は明示的にゼロ書き込みする
        //             _proxy.AccumulateValue(key, 0);
        //         }
        //     }
        //     _proxy.Apply();
        //     _eyeBoneResetter.ReserveReset = true;
        // }

        /// <summary> 現在このコンポーネントが適用すべきブレンドシェイプを持ってるかどうか </summary>
        public bool HasBlendShapeToApply => _blendShape.Count > 0;
        
        public void Accumulate(VRMBlendShapeProxy proxy)
        {
            if (!HasBlendShapeToApply)
            {
                //オーバーライド不要なので何もしない
                return;
            }

            //NOTE: LateUpdateの実装(初期実装)と違い、必要なとこだけ狙ってAccumulateする
            for (int i = 0; i < _allBlendShapeKeys.Length; i++)
            {
                var key = _allBlendShapeKeys[i];
                //リップシンク保持オプションがオン = AIUEOはAccumulateしない(元の値をリスペクトする)
                if (SkipLipSyncKeys && _lipSyncKeys.Contains(key))
                {
                    continue;
                }
                    
                if (_blendShape.TryGetValue(key, out float value) && value > 0f)
                {
                    proxy.AccumulateValue(key, value);
                }
            }
            _eyeBoneResetter.ReserveReset = true;
        }
    }
}
