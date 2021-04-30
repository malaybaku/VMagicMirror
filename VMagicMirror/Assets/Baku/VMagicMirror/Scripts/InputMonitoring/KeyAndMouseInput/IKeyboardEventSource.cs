using System;

namespace Baku.VMagicMirror
{
    public interface IKeyMouseEventSource
    {
        IObservable<string> PressedRawKeys { get; }
        IObservable<string> KeyDown { get; }
        IObservable<string> KeyUp { get; }
        IObservable<string> MouseButton { get; }
    }

    public static class MouseButtonEventNames
    {
        public const string RDown = nameof(RDown);
        public const string MDown = nameof(MDown);
        public const string LDown = nameof(LDown);
        public const string RUp = nameof(RUp);
        public const string MUp = nameof(MUp);
        public const string LUp = nameof(LUp);
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
