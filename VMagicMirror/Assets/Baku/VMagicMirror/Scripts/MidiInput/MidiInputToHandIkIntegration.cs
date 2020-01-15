using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public class MidiInputToHandIkIntegration : MonoBehaviour
    {
        [SerializeField] private MidiInputObserver observer = null;
        [SerializeField] private HandIKIntegrator handIk = null;

        private void Start()
        {
            observer.NoteOn.Subscribe(handIk.NoteOn);
            observer.KnobValue.Subscribe(
                v => handIk.KnobValueChange(v.Item1, v.Item2)
            );


        }
    }
}
