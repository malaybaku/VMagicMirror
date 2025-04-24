using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.WordToMotion
{
    /// <summary>
    /// MIDI入力を元にWtMの発火をリクエストするやつ
    /// </summary>
    public class MidiRequestSource : PresenterBase, IRequestSource
    {
        public SourceType SourceType => SourceType.Midi;

        private readonly Subject<int> _runMotionRequested = new Subject<int>();
        public IObservable<int> RunMotionRequested => _runMotionRequested;

        private bool _isActive = false;
        public void SetActive(bool active) => _isActive = active;

        private readonly IMessageReceiver _receiver;
        private readonly IMessageSender _sender;
        private readonly MidiInputObserver _midiObserver;
        
        private readonly Dictionary<int, int> _noteNumberToMotionMap = new Dictionary<int, int>();
        //NOTE: ちょっと横着で、設定に応じてついでにWPF側へのエコーバックもやってあげる
        private bool _redirectNoteOnMessageToIpc = false;

        public MidiRequestSource(
            IMessageReceiver receiver, IMessageSender sender, MidiInputObserver midiObserver)
        {
            _receiver = receiver;
            _sender = sender;
            _midiObserver = midiObserver;
        }

        public override void Initialize()
        {
            _receiver.AssignCommandHandler(
                VmmCommands.LoadMidiNoteToMotionMap,
                c => LoadMidiNoteToMotionMap(c.GetStringValue())
                );
            _receiver.AssignCommandHandler(
                VmmCommands.RequireMidiNoteOnMessage,
                c => _redirectNoteOnMessageToIpc = c.ToBoolean()
                );

            _midiObserver.NoteOn
                .Subscribe(noteNumber =>
                {
                    if (!_isActive)
                    {
                        return;
                    }

                    if (_redirectNoteOnMessageToIpc)
                    {
                        _sender.SendCommand(MessageFactory.Instance.MidiNoteOn(noteNumber));
                    }

                    if (_noteNumberToMotionMap.ContainsKey(noteNumber))
                    {
                        _runMotionRequested.OnNext(_noteNumberToMotionMap[noteNumber]);
                    }
                })
                .AddTo(this);
        }
        
        private void LoadMidiNoteToMotionMap(string mapObject)
        {
            _noteNumberToMotionMap.Clear();
            try
            {
                var map = JsonUtility.FromJson<MidiNoteToMotionMap>(mapObject);
                foreach (var item in map.Items)
                {
                    //ノートからインデックス引くのでこうなる: コンフィグの表示と逆方向のマッピングになる。
                    //同じノートに二重登録しようとしているとヘンな動きになるが、これは仕様
                    _noteNumberToMotionMap[item.NoteNumber] = item.ItemIndex;
                }
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
                _noteNumberToMotionMap.Clear();
            }
        }
    }
}
