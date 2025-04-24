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

        private void OnReceive(CommandReceivedData e)
        {
            if (e.Command is not VMagicMirror.VmmServerCommands.MidiNoteOn)
            {
                return;
            }

            if (int.TryParse(e.GetStringValue(), out var noteNumber))
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
