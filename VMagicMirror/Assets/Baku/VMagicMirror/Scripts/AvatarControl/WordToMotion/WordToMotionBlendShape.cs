using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using VRM;
using Zenject;

namespace Baku.VMagicMirror
{
    public readonly struct WordToMotionBlendShapeApplyContent
    {
        private WordToMotionBlendShapeApplyContent(bool hasValue, List<(BlendShapeKey, float)> keys, bool keepLipSync)
        {
            HasValue = hasValue;
            Keys = keys;
            KeepLipSync = keepLipSync;
        }
        
        public static WordToMotionBlendShapeApplyContent Empty { get; } 
            = new WordToMotionBlendShapeApplyContent(false, new List<(BlendShapeKey, float)>(), false);

        public static WordToMotionBlendShapeApplyContent Create(List<(BlendShapeKey, float)> keys, bool keepLipSync)
            => new WordToMotionBlendShapeApplyContent(true, keys, keepLipSync);

        public bool HasValue { get; }
        public List<(BlendShapeKey, float)> Keys { get; }
        public bool KeepLipSync { get; }
        
    }
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
            BlendShapeKey.CreateFromPreset(BlendShapePreset.A),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.I),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.U),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.E),
            BlendShapeKey.CreateFromPreset(BlendShapePreset.O),
        };
        
        private BlendShapeKey[] _allBlendShapeKeys = new BlendShapeKey[0];

        private readonly Dictionary<BlendShapeKey, float> _blendShape = new Dictionary<BlendShapeKey, float>();
        
        private EyeBonePostProcess _eyeBoneResetter;

        private bool _hasDiff;

        private readonly List<(BlendShapeKey, float)> _currentValueSource = new List<(BlendShapeKey, float)>(8);
        private readonly ReactiveProperty<WordToMotionBlendShapeApplyContent> _currentValue
            = new ReactiveProperty<WordToMotionBlendShapeApplyContent>(WordToMotionBlendShapeApplyContent.Empty);
        public IReadOnlyReactiveProperty<WordToMotionBlendShapeApplyContent> CurrentValue => _currentValue;
        
        [Inject]
        public void Initialize(EyeBonePostProcess eyeBoneResetter)
        {
            _eyeBoneResetter = eyeBoneResetter;
        }
        
        public void Initialize(VRMBlendShapeProxy proxy)
        {
            _allBlendShapeKeys = proxy
                .BlendShapeAvatar
                .Clips
                .Select(c => BlendShapeKeyFactory.CreateFrom(c.BlendShapeName))
                .ToArray();
        }

        public void DisposeProxy()
        {
            _allBlendShapeKeys = new BlendShapeKey[0];
        }

        /// <summary> trueの場合、このスクリプトではリップシンクのブレンドシェイプに書き込みを行いません。 </summary>
        public bool KeepLipSync { get; set; }

        public void UpdateCurrentValue()
        {
            if (!_hasDiff)
            {
                return;
            }

            _currentValueSource.Clear();
            foreach (var pair in _blendShape)
            {
                _currentValueSource.Add((pair.Key, pair.Value));
            }

            if (_currentValueSource.Count > 0)
            {
                _currentValue.Value = WordToMotionBlendShapeApplyContent.Create(_currentValueSource, KeepLipSync);
            }
            else
            {
                _currentValue.Value = WordToMotionBlendShapeApplyContent.Empty;
            }
        }

        /// <summary>
        /// Word To Motionによるブレンドシェイプを指定します。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <remarks>1つ以上のブレンドシェイプを指定すると通常の表情制御をオーバーライドする。</remarks>
        public void Add(BlendShapeKey key, float value)
        {
            if (_allBlendShapeKeys.Any(k => k.Name == key.Name) && !_blendShape.ContainsKey(key))
            {
                _blendShape[key] = value;
                _hasDiff = true;
            }
        }

        /// <summary>Word To Motionによる表情制御を無効化(終了)します。</summary>
        public void Clear()
        {
            if (_blendShape.Count > 0)
            {
                _hasDiff = true;
            }
            _blendShape.Clear();
        }

        public void ResetBlendShape()
        {
            if (_blendShape.Count > 0)
            {
                Clear();
            }
        }

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
                if (KeepLipSync && _lipSyncKeys.Any(k => k.Preset == key.Preset && k.Name == key.Name))
                {
                    continue;
                }
                //NOTE: パーフェクトシンクのクリップはリップシンク保持であっても通す。
                //これはフィルタすると重すぎるので「パーフェクトシンク使う人はそのくらい理解してくれ」という意味です
                if (_blendShape.TryGetValue(key, out float value) && value > 0f)
                {
                    proxy.AccumulateValue(key, value);
                }
            }
            _eyeBoneResetter.ReserveReset = true;
        }
    }
}
