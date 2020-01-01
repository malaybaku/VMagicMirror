using System;
using UnityEngine;
using Zenject;
using UniRx;
using MidiJack;

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

        private void OnDestroy()
        {
            WindowsMidiInterop.Instance.SetActive(false);
        }

        private void SetMidiReadEnable(bool enable) 
            => WindowsMidiInterop.Instance.SetActive(enable);
    }
}
