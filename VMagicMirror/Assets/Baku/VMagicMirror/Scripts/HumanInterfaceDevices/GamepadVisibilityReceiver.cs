using Deform;
using DG.Tweening;
using UnityEngine;
using Zenject;
using UniRx;

namespace Baku.VMagicMirror
{
    [RequireComponent(typeof(MagnetDeformer))]
    public class GamepadVisibilityReceiver : MonoBehaviour
    {
        [Inject] private ReceivedMessageHandler _handler;
        [Inject] private DeformableCounter _deformableCounter;

        private MagnetDeformer _deformer = null;
        private Renderer[] _renderers = new Renderer[0];
        private bool _latestVisibility = false;
        
        private void Start()
        {
            _handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.GamepadVisibility:
                        SetGamepadVisibility(message.ToBoolean());
                        break;
                }
            });

            _deformer = GetComponent<MagnetDeformer>();
            _renderers = GetComponentsInChildren<Renderer>();
        }

        private void SetGamepadVisibility(bool visible)
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
                        r.enabled = _latestVisibility;
                    }
                });
        }
    }
}
