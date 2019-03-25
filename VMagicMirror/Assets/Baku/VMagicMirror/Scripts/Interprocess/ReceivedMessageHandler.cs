using System;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public class ReceivedMessageHandler : MonoBehaviour
    {
        private readonly Subject<ReceivedMessage> _messageSubject = new Subject<ReceivedMessage>();
        public IObservable<ReceivedMessage> Messages => _messageSubject;

        public void Receive(string message)
        {
            string command = message.Split(':')[0];
            string content = message.Substring(command.Length + 1);
            _messageSubject.OnNext(new ReceivedMessage(command, content));
        }
    }
}

