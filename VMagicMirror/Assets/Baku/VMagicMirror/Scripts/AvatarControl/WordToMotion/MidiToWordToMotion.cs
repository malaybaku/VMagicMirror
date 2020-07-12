using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public sealed class MidiToWordToMotion 
    {
        public MidiToWordToMotion(IMessageReceiver receiver, IMessageSender sender, MidiInputObserver midiObserver)
        {
            receiver.AssignCommandHandler(
                VmmCommands.LoadMidiNoteToMotionMap,
                c => LoadMidiNoteToMotionMap(c.Content)
                );
            receiver.AssignCommandHandler(
                VmmCommands.RequireMidiNoteOnMessage,
                c => _redirectNoteOnMessageToIpc = c.ToBoolean()
                );
            
            _midiObserver = midiObserver.NoteOn.Subscribe(noteNumber =>
            {
                if (_redirectNoteOnMessageToIpc)
                {
                    sender.SendCommand(MessageFactory.Instance.MidiNoteOn(noteNumber));
                }
                
                if (_noteNumberToMotionMap.ContainsKey(noteNumber))
                {
                    RequestExecuteWordToMotionItem?.Invoke(_noteNumberToMotionMap[noteNumber]);
                }
            });            
        }

        private readonly IDisposable _midiObserver;
        private readonly Dictionary<int, int> _noteNumberToMotionMap = new Dictionary<int, int>();

        private bool _redirectNoteOnMessageToIpc = false;

        /// <summary>Word to Motionの要素を実行してほしいとき、アイテムのインデックスを引数にして発火する。</summary>
        public event Action<int> RequestExecuteWordToMotionItem;
        
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
    
    /// <summary>
    /// Word to Motionのアイテムのインデックスと、MIDIのノート番号との対応を取るためのアイテム
    /// Unity側でJsonUtilityで読めるようなフォーマットにしてます
    /// </summary>
    [Serializable]
    public class MidiNoteToMotionMap
    {
        public List<MidiNoteToMotionItem> Items;
    }

    [Serializable]
    public class MidiNoteToMotionItem
    {
        public int ItemIndex;
        public int NoteNumber;
    }
}
