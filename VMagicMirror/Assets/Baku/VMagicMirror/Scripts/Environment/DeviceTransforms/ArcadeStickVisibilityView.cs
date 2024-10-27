using Deform;
using DG.Tweening;
using UnityEngine;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(MagnetDeformer))]
    public class ArcadeStickVisibilityView : MonoBehaviour
    {
        private DeformableCounter _deformableCounter;
        private MagnetDeformer _deformer;
        private Renderer[] _renderers;

        public bool IsVisible { get; private set; } = false;

        public void Setup(DeformableCounter deformableCounter)
        {
            _deformableCounter = deformableCounter;
            _deformer = GetComponent<MagnetDeformer>();
            _renderers = GetComponentsInChildren<Renderer>();
        }

        public void SetVisible(bool visible)
        {
            if (visible == IsVisible)
            {
                return;
            }

            IsVisible = visible;
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
                        foreach (var r in _renderers)
                        {
                            r.enabled = true;
                        }
                    }
                })
                .OnComplete(() =>
                {
                    _deformableCounter.Decrement();
                    foreach (var r in _renderers)
                    {
                        r.enabled = IsVisible;
                    }
                });
        }
    }
}
