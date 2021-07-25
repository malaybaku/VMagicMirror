using System.Collections.Generic;
using System.Linq;
using Baku.VMagicMirror.ExternalTracker;
using UnityEngine;
using VRM;

namespace  Baku.VMagicMirror
{
    public class PerfectSyncValueVisualizer : MonoBehaviour
    {
        [SerializeField] private ExternalTrackerPerfectSync source = null;
        [SerializeField] private PerfectSyncValueIndicatorItem itemPrefab = null;
        [SerializeField] private Transform itemsParent = null;
        [SerializeField] private bool fixOnMaxValue = false;

        private BlendShapeKey[] _keys;
        private Dictionary<BlendShapeKey, float> _values;
        private Dictionary<BlendShapeKey, float> _valuesOnFrame;
        private Dictionary<BlendShapeKey, PerfectSyncValueIndicatorItem> _items;
        
        public void Accumulate(BlendShapeKey key, float value) => _valuesOnFrame[key] += value;

        private void Start()
        {
            _keys = ExternalTrackerPerfectSync.Keys.PerfectSyncKeys;
            _items = _keys.OrderBy(k => k.Name)
                .ToDictionary(
                    k => k,
                    k =>
                    {
                        var result = Instantiate(itemPrefab, itemsParent);
                        result.SetBlendShapeName(k.Name);
                        return result;
                    });

            _values = _keys.ToDictionary(k => k, k => 0f);
            _valuesOnFrame = _keys.ToDictionary(k => k, k => 0f);
            source.ValueAccumulated += Accumulate;
        }

        private void LateUpdate()
        {
            foreach (var key in _keys)
            {
                if (fixOnMaxValue)
                {
                    _values[key] = Mathf.Max(_values[key], _valuesOnFrame[key]);
                }
                else
                {
                    
                    _values[key] = _valuesOnFrame[key];
                }
                _items[key].SetValue(_values[key]);
                _valuesOnFrame[key] = 0f;
            }
        }
    }
}
