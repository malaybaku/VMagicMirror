using System;
using UnityEngine;
using UniRx;
using MidiJack;

namespace Baku.VMagicMirror
{
    public class MidiInputObserver : MonoBehaviour
    {
        //NOTE: MIDI入力はWPF側からUWPのAPIで読み取った方がいいかもしれないので、気になったら変える
        private readonly Subject<int> _noteOn = new Subject<int>();
        public IObservable<int> NoteOn => _noteOn;
        
        private readonly Subject<(int, float)> _knobValue = new Subject<(int, float)>();
        public IObservable<(int, float)> KnobValue => _knobValue;
        
        private void Start()
        {
            MidiMaster.noteOnDelegate += OnNoteOn;
            MidiMaster.knobDelegate += OnKnobValue;
        }

        private void OnNoteOn(MidiChannel channel, int note, float velocity) 
            => _noteOn.OnNext(note);

        private void OnKnobValue(MidiChannel channel, int knobNumber, float knobValue) 
            => _knobValue.OnNext((knobNumber, knobValue));
    }
}
