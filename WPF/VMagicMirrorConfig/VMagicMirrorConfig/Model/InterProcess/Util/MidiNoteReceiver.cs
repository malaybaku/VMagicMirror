using System;

namespace Baku.VMagicMirrorConfig
{
    /// <summary> MIDIのNoteOnメッセージを受け取ってくれるすごいやつだよ </summary>
    internal class MidiNoteReceiver
    {
        public MidiNoteReceiver(IMessageReceiver receiver)
        {
            _receiver = receiver;
        }
        private readonly IMessageReceiver _receiver;

        public event EventHandler<MidiNoteEventArgs>? MidiNoteOn;

        public void Start() => _receiver.ReceivedCommand += OnReceive;
        public void End() => _receiver.ReceivedCommand -= OnReceive;

        private void OnReceive(object? sender, CommandReceivedEventArgs e)
        {
            if (e.Command != ReceiveMessageNames.MidiNoteOn)
            {
                return;
            }

            if (int.TryParse(e.Args, out var noteNumber))
            {
                MidiNoteOn?.Invoke(this, new MidiNoteEventArgs(noteNumber));
            }
        }
    }

    public class MidiNoteEventArgs : EventArgs
    {
        public MidiNoteEventArgs(int note)
        {
            Note = note;
        }
        public int Note { get; }
    }
}
