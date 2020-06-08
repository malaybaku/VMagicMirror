using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using UniRx;

namespace Baku.VMagicMirror
{
    public class MidiToWordToMotion : MonoBehaviour
    {
        private readonly Dictionary<int, int> _noteNumberToMotionMap = new Dictionary<int, int>();

        [Tooltip("ボタン押下イベントが押されたらしばらくイベント送出をストップするクールダウンタイム")]
        [SerializeField] private float cooldownTime = 0.3f;

        [Inject]
        public void Initialize(ReceivedMessageHandler handler, IMessageSender sender, MidiInputObserver midiObserver)
        {
            _handler = handler;
            _sender = sender;
            _midiInputObserver = midiObserver;
        }

        private ReceivedMessageHandler _handler;
        private IMessageSender _sender;
        private MidiInputObserver _midiInputObserver = null;

        private bool _redirectNoteOnMessageToIpc = false;

        /// <summary>Word to Motionの要素を実行してほしいとき、アイテムのインデックスを引数にして発火する。</summary>
        public event Action<int> RequestExecuteWordToMotionItem;
        
        public bool UseMidiInput { get; set; } = false;
        private float _cooldownCount = 0;
        
        private void Start()
        {
            _handler.Commands.Subscribe(c =>
            {
                switch (c.Command)
                {
                    case MessageCommandNames.LoadMidiNoteToMotionMap:
                        LoadMidiNoteToMotionMap(c.Content);
                        break;
                    case MessageCommandNames.RequireMidiNoteOnMessage:
                        _redirectNoteOnMessageToIpc = c.ToBoolean();
                        break;
                }
            });
            
            _midiInputObserver.NoteOn.Subscribe(noteNumber =>
            {
                if (_redirectNoteOnMessageToIpc)
                {
                    _sender?.SendCommand(MessageFactory.Instance.MidiNoteOn(noteNumber));
                }
                
                if (UseMidiInput && 
                    _cooldownCount <= 0 && 
                    _noteNumberToMotionMap.ContainsKey(noteNumber))
                {
                    RequestExecuteWordToMotionItem?.Invoke(_noteNumberToMotionMap[noteNumber]);
                    _cooldownCount = cooldownTime;
                }
            });
        }

        private void Update()
        {
            if (_cooldownCount > 0)
            {
                _cooldownCount -= Time.deltaTime;
            }
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
