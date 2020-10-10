using System;

namespace Baku.VMagicMirror
{
    public interface IKeyMouseEventSource
    {
        IObservable<string> PressedRawKeys { get; }
        IObservable<string> PressedKeys { get; }
        IObservable<string> MouseButton { get; }
    }

    public static class RandomKeyboardKeys
    {
        //NOTE: ランダム打鍵で全部のキーを叩かせる理由がない(それだと腕が動きすぎる懸念がある)ので絞っておく
        public static readonly string[] RandomKeyNames　= new []
        {
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", 
            "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
            "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "D0", 
        };
    }
}
