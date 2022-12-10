using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    public readonly struct WordToMotionBlendShapeApplyContent
    {
        private WordToMotionBlendShapeApplyContent(
            bool hasValue, List<(ExpressionKey, float)> keys, bool keepLipSync, bool isPreview)
        {
            HasValue = hasValue;
            Keys = keys;
            KeepLipSync = keepLipSync;
            IsPreview = isPreview;
        }
        
        public static WordToMotionBlendShapeApplyContent Empty { get; } 
            = new WordToMotionBlendShapeApplyContent(false, new List<(ExpressionKey, float)>(), false, false);

        public static WordToMotionBlendShapeApplyContent Create(List<(ExpressionKey, float)> keys, bool keepLipSync,
            bool isPreview) => new WordToMotionBlendShapeApplyContent(true, keys, keepLipSync, isPreview);

        public bool HasValue { get; }
        public List<(ExpressionKey, float)> Keys { get; }
        public bool KeepLipSync { get; }
        public bool IsPreview { get; }
        
    }

    /// <summary> Word To Motionのブレンドシェイプを適用する。 </summary>
    public class WordToMotionBlendShape : MonoBehaviour
    {
        private static readonly ExpressionKey[] _lipSyncKeys = new []
        {
            ExpressionKey.CreateFromPreset(ExpressionPreset.aa),
            ExpressionKey.CreateFromPreset(ExpressionPreset.ih),
            ExpressionKey.CreateFromPreset(ExpressionPreset.ou),
            ExpressionKey.CreateFromPreset(ExpressionPreset.ee),
            ExpressionKey.CreateFromPreset(ExpressionPreset.oh),
        };
        
        private ExpressionKey[] _allBlendShapeKeys = Array.Empty<ExpressionKey>();

        private readonly Dictionary<ExpressionKey, float> _blendShape = new Dictionary<ExpressionKey, float>();
        
        private EyeBoneAngleSetter _eyeBoneResetter;

        private bool _hasDiff;

        private readonly List<(ExpressionKey, float)> _currentValueSource = new List<(ExpressionKey, float)>(8);
        private readonly Subject<WordToMotionBlendShapeApplyContent> _currentValue = new Subject<WordToMotionBlendShapeApplyContent>();
        public IObservable<WordToMotionBlendShapeApplyContent> CurrentValue => _currentValue;
        
        [Inject]
        public void Initialize(EyeBoneAngleSetter eyeBoneResetter)
        {
            _eyeBoneResetter = eyeBoneResetter;
        }
        
        public void Initialize(VRM10ObjectExpression expression)
        {
            _allBlendShapeKeys = expression.LoadExpressionMap().Keys.ToArray();
        }

        public void DisposeProxy()
        {
            _allBlendShapeKeys = Array.Empty<ExpressionKey>();
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
        public void SetForPreview(IEnumerable<(ExpressionKey, float)> values, bool keepLipSync)
        {
            //SetBlendShapesとは違ってClearしない: 前回と同じ値でよい場合、_hasDiff == falseになるようにしたい
            foreach (var (key, value) in values)
            {
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
        public void SetBlendShapes(IEnumerable<(ExpressionKey, float)> values, bool keepLipSync)
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
        
        public void Accumulate(ExpressionAccumulator accumulator)
        {
            if (!HasBlendShapeToApply)
            {
                //オーバーライド不要なので何もしない
                return;
            }

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
                    accumulator.Accumulate(key, value);
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
