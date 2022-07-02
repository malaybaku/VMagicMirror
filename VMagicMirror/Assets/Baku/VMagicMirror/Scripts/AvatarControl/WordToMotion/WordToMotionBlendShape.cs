using System;
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
        private WordToMotionBlendShapeApplyContent(
            bool hasValue, List<(BlendShapeKey, float)> keys, bool keepLipSync, bool isPreview)
        {
            HasValue = hasValue;
            Keys = keys;
            KeepLipSync = keepLipSync;
            IsPreview = isPreview;
        }
        
        public static WordToMotionBlendShapeApplyContent Empty { get; } 
            = new WordToMotionBlendShapeApplyContent(false, new List<(BlendShapeKey, float)>(), false, false);

        public static WordToMotionBlendShapeApplyContent Create(List<(BlendShapeKey, float)> keys, bool keepLipSync,
            bool isPreview) => new WordToMotionBlendShapeApplyContent(true, keys, keepLipSync, isPreview);

        public bool HasValue { get; }
        public List<(BlendShapeKey, float)> Keys { get; }
        public bool KeepLipSync { get; }
        public bool IsPreview { get; }
        
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
        
        private EyeBoneAngleSetter _eyeBoneResetter;

        private bool _hasDiff;

        private readonly List<(BlendShapeKey, float)> _currentValueSource = new List<(BlendShapeKey, float)>(8);
        private readonly Subject<WordToMotionBlendShapeApplyContent> _currentValue = new Subject<WordToMotionBlendShapeApplyContent>();
        public IObservable<WordToMotionBlendShapeApplyContent> CurrentValue => _currentValue;
        
        [Inject]
        public void Initialize(EyeBoneAngleSetter eyeBoneResetter)
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
        public bool KeepLipSync { get; private set; }

        private bool _isPreview = false;
        /// <summary> 現在保持している値がPreview用のクリップの場合はtrue </summary>
        public bool IsPreview
        {
            get => _isPreview;
            set
            {
                if (_isPreview == value)
                {
                    return;
                }
                _isPreview = value;
                _hasDiff = true;
            }
        }

        public void UpdateCurrentValue()
        {
            if (!_hasDiff)
            {
                return;
            }

            _hasDiff = false;
            _currentValueSource.Clear();
            foreach (var pair in _blendShape)
            {
                _currentValueSource.Add((pair.Key, pair.Value));
            }

            if (_currentValueSource.Count > 0)
            {
                _currentValue.OnNext(WordToMotionBlendShapeApplyContent.Create(_currentValueSource, KeepLipSync, IsPreview));
            }
            else
            {
                _currentValue.OnNext(WordToMotionBlendShapeApplyContent.Empty);
            }
        }

        /// <summary>
        /// Preview用のクリップ情報があるとき、毎フレーム呼び出します。
        /// </summary>
        /// <param name="values"></param>
        /// <param name="keepLipSync"></param>
        public void SetForPreview(IEnumerable<(BlendShapeKey, float)> values, bool keepLipSync)
        {
            //SetBlendShapesとは違ってClearしない: 前回と同じ値でよい場合、_hasDiff == falseになるようにしたい
            foreach (var (key, value) in values)
            {
                //NOTE: Removeは不要。ロード中のアバターのClipはぜんぶ飛んでくるはずのため
                if (_allBlendShapeKeys.Any(k => k.Name == key.Name) && 
                    (!_blendShape.ContainsKey(key) || Mathf.Abs(_blendShape[key] - value) > 0.005f)
                   )
                {
                    _blendShape[key] = value;
                    _hasDiff = true;
                }
            }
            
            if (KeepLipSync != keepLipSync)
            {
                KeepLipSync = keepLipSync;
                _hasDiff = true;
            }
        }

        /// <summary>
        /// Preview用ではない表情を適用する時、開始時に1回だけ呼び出すことで、表情を更新します。
        /// </summary>
        /// <param name="values"></param>
        /// <param name="keepLipSync"></param>
        public void SetBlendShapes(IEnumerable<(BlendShapeKey, float)> values, bool keepLipSync)
        {
            Clear();
            foreach (var (key, value) in values)
            {
                _blendShape[key] = value;
                //valuesがカラになるパターンはほぼ無いはずで、実質的にはいつも_hasDiff == trueになる
                _hasDiff = true;
            }

            if (KeepLipSync != keepLipSync)
            {
                KeepLipSync = keepLipSync;
                _hasDiff = true;
            }
        }
        
        /// <summary>
        /// Word To Motionによる表情制御(Preview表示や通常の適用)の終了時に呼び出します。
        /// </summary>
        public void ResetBlendShape()
        { 
            Clear();
            if (KeepLipSync)
            {
                KeepLipSync = false;
                _hasDiff = true;
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

        private void Clear()
        {
            if (_blendShape.Count > 0)
            {
                _hasDiff = true;
            }
            _blendShape.Clear();
        }
    }
}
