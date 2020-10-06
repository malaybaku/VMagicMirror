using System;

namespace Baku.VMagicMirror
{
    public interface IKeyMouseEventSource
    {
        IObservable<string> PressedRawKeys { get; }
        IObservable<string> PressedKeys { get; }
        IObservable<string> MouseButton { get; }
    }
}
