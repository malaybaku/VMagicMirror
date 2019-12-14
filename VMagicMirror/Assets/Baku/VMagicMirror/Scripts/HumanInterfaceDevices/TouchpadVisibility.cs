using Deform;
using DG.Tweening;
using UnityEngine;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(MagnetDeformer))]
    public class TouchpadVisibility : MonoBehaviour
    {
        private MagnetDeformer _deformer = null;
        private Renderer[] _renderers = null;

        private void Start()
        {
            _deformer = GetComponent<MagnetDeformer>();
            _renderers = GetComponentsInChildren<Renderer>();
        }

        public void SetVisibility(bool visible)
        {
            DOTween
                .To(
                    () => _deformer.Factor, 
                    v => _deformer.Factor = v, 
                    visible ? 0.0f : 1.0f, 
                    0.5f)
                .SetEase(Ease.OutCubic)
                .OnStart(() =>
                {
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
                    foreach (var r in _renderers)
                    {
                        r.enabled = visible;
                    }
                });
        }
    }
}
