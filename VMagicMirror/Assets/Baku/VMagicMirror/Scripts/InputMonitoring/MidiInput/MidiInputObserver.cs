using System;
using UnityEngine;
using R3;
using MidiJack;

namespace Baku.VMagicMirror
{
    public class MidiInputObserver : MonoBehaviour
    {
        //NOTE: MIDI入力はWPF側からUWPのAPIで読み取った方がいいかもしれないので、気になったら変える
        private readonly Subject<int> _noteOn = new Subject<int>();
        public Observable<int> NoteOn => _noteOn;
        
        private readonly Subject<(int, float)> _knobValue = new Subject<(int, float)>();
        public Observable<(int, float)> KnobValue => _knobValue;
        
        private void Start()
        {
            MidiMaster.noteOnDelegate += OnNoteOn;
            MidiMaster.knobDelegate += OnKnobValue;
        }

        private void OnNoteOn(MidiChannel channel, int note, float velocity)
        {
            //note on, velocity = 0 の組み合わせは実際にはノートオフを表しているため、通過させない。
            //しきい値の0.001fにはあまり深い意味はなく、1/127より小さいかどうかチェックできてれば十分
            if (velocity > 0.001f)
            {
                _noteOn.OnNext(note);
            }
        }

        private void OnKnobValue(MidiChannel channel, int knobNumber, float knobValue) 
            => _knobValue.OnNext((knobNumber, knobValue));
    }
}
