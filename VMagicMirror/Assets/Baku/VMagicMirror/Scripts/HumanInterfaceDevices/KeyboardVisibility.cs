using Deform;
using DG.Tweening;
using UnityEngine;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(MagnetDeformer))]
    [RequireComponent(typeof(Renderer))]
    public class KeyboardVisibility : MonoBehaviour
    {
        private MagnetDeformer _deformer = null;
        private Renderer _renderer = null;

        private void Start()
        {
            _deformer = GetComponent<MagnetDeformer>();
            _renderer = GetComponent<Renderer>();   
        }

        public void SetVisibility(bool visible)
        {
            DOTween
                .To(
                    () => _deformer.Factor,
                    v => _deformer.Factor = v,
                    visible ? 0.0f : 0.5f,
                    0.5f)
                .SetEase(Ease.OutCubic)
                .OnStart(() =>
                {
                    if (visible)
                    {
                        _renderer.enabled = true;
                    }
                })
                .OnComplete(() => _renderer.enabled = visible);
        }
    }
}

