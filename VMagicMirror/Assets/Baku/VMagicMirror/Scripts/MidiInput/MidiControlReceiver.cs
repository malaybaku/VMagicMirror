using UnityEngine;
using Zenject;
using UniRx;

namespace Baku.VMagicMirror
{
    public class MidiControlReceiver : MonoBehaviour
    {
        [Inject] private ReceivedMessageHandler _handler;

        private void Start()
        {
            //NOTE: WPFとUnityどっちで入力リードするか未確定なので不明。何も要らない可能性もあり。
            _handler.Commands.Subscribe(command =>
            {
                switch (command.Command)
                {
                    case MessageCommandNames.EnableMidiRead:
                        SetMidiReadEnable(command.ToBoolean());
                        break;
                }
            });
        }

        private void SetMidiReadEnable(bool enable)
        {
            //TODO: ここでMidiMasterをブッ殺したい…ブッ殺したくない？
//            MidiMaster.
        }
    }
}
