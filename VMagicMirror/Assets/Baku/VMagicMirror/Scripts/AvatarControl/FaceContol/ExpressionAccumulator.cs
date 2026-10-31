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
        private readonly Dictionary<ExpressionKey, float> _values = new();
        private readonly HashSet<ExpressionKey> _keys = new();
        private readonly IVRMLoadable _vrmLoadable;
        private readonly PerfectSyncExpressionRepository _expressions;

        private bool _hasModel;
        private Vrm10RuntimeExpression _expression;
        
        public event Action<IReadOnlyDictionary<ExpressionKey, float>> PreApply; 

        public ExpressionAccumulator(IVRMLoadable vrmLoadable, PerfectSyncExpressionRepository expressions)
        {
            _vrmLoadable = vrmLoadable;
            _expressions = expressions;
        }

        public void Initialize()
        {
            _expressions.ExpressionsChanged += () =>
            {
                _expression = _expressions.Instance.Runtime.Expression;
                var exprMap = _expressions.EffectiveMap;

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
                _values.Clear();
                _keys.Clear();
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
            _expression?.SetWeightsNonAlloc(_values);
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
        public IReadOnlyDictionary<ExpressionKey, float> GetValues() => _values;
    }
}
