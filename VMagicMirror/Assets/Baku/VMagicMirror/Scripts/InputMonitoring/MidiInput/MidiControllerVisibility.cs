using System.Linq;
using Deform;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(MagnetDeformer))]
    [RequireComponent(typeof(Renderer))]
    public class MidiControllerVisibility : MonoBehaviour
    {
        [Inject] private DeformableCounter _deformableCounter = null;

        
        private MagnetDeformer _deformer = null;
        private Renderer _renderer = null;
        private bool _latestVisibility = false;
        private Renderer[] _knobRenderers = new Renderer[0];
        
        public bool IsVisible => _latestVisibility;
        
        private void Start()
        {
            _deformer = GetComponent<MagnetDeformer>();
            _renderer = GetComponent<Renderer>();
            var midiController = GetComponent<MidiControllerProvider>();
            _knobRenderers = midiController
                .Knobs
                .Select(k => k.GetComponentInChildren<Renderer>())
                .ToArray();
            foreach (var deformable in midiController
                .Knobs
                .Select(k => k.GetComponentInChildren<Deformable>()))
            {
                deformable.AddDeformer(_deformer);
            }
            SetVisibility(false);
        }

        public void SetVisibility(bool visible)
        {
            _latestVisibility = visible;
            DOTween
                .To(
                    () => _deformer.Factor,
                    v => _deformer.Factor = v,
                    visible ? 0.0f : 0.5f,
                    0.5f)
                .SetEase(Ease.OutCubic)
                .OnStart(() =>
                {
                    _deformableCounter.Increment();
                    if (visible)
                    {
                        _renderer.enabled = true;
                        for (int i = 0; i < _knobRenderers.Length; i++)
                        {
                            _knobRenderers[i].enabled = true;
                        }
                    }
                })
                .OnComplete(() =>
                {
                    _renderer.enabled = _latestVisibility;
                    for (int i = 0; i < _knobRenderers.Length; i++)
                    {
                        _knobRenderers[i].enabled = _latestVisibility;
                    }
                    _deformableCounter.Decrement();
                });
        }
    }
}