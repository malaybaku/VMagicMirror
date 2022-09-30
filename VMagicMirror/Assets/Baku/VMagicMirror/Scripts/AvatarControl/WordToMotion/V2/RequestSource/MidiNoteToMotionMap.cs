using System;
using System.Collections.Generic;

namespace Baku.VMagicMirror.WordToMotion
{
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
