using UnityEngine;
using Zenject;
using MidiJack;

namespace Baku.VMagicMirror
{
    public class MidiControlReceiver : MonoBehaviour
    {
        //TODO: 非MonoBehaviour化 + OnDestroyの所もいい感じにしたい
        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.EnableMidiRead,
                command => SetMidiReadEnable(command.ToBoolean())
                );
        }

        private void OnDestroy()
        {
            WindowsMidiInterop.Instance.SetActive(false);
        }

        private void SetMidiReadEnable(bool enable) 
            => WindowsMidiInterop.Instance.SetActive(enable);
    }
}
