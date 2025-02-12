using System;
using System.Collections.Generic;
using UniVRM10;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 表情のBlendShapeについて「Accumulate -> Apply」というVRM 0.x用の操作をサポートするための、
    /// VRM10のRuntime.Expressionのラッパー
    /// </summary>
    public class ExpressionAccumulator : IInitializable
    {
        private readonly Dictionary<ExpressionKey, float> _values = new Dictionary<ExpressionKey, float>();
        private readonly HashSet<ExpressionKey> _keys = new HashSet<ExpressionKey>();
        private readonly IVRMLoadable _vrmLoadable;

        private bool _hasModel;
        private Vrm10RuntimeExpression _expression;
        
        public event Action<IReadOnlyDictionary<ExpressionKey, float>> PreApply; 

        public ExpressionAccumulator(IVRMLoadable vrmLoadable)
        {
            _vrmLoadable = vrmLoadable;
        }

        public void Initialize()
        {
            _vrmLoadable.VrmLoaded += info =>
            {
                _expression = info.instance.Runtime.Expression;
                var exprMap = info.instance.Vrm.Expression.LoadExpressionMap();

                _values.Clear();
                _keys.Clear();
                foreach (var key in exprMap.Keys)
                {
                    _values[key] = 0f;
                    _keys.Add(key);
                }
                _hasModel = true;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _expression = null;
            };
        }

        public void Accumulate(ExpressionKey key, float value)
        {
            if (_hasModel && _keys.Contains(key))
            {
                _values[key] += value;
            }
        }

        public void Apply()
        {
            PreApply?.Invoke(_values);
            _expression?.SetWeights(_values);
        }

        public void ResetValues()
        {
            foreach (var k in _keys)
            {
                _values[k] = 0f;
            }
        }

        // NOTE: HasKeyやGetValueはモデルがロード中じゃないとfalse/0を返す
        public bool HasKey(ExpressionKey key) => _values.ContainsKey(key);
        public float GetValue(ExpressionKey key) => _values.TryGetValue(key, out var result) ? result : 0f;
    }
}
