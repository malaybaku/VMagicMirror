using System;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.GameInput
{
    public interface IGameInputSourceSwitcher
    {
        IGameInputSource[] Sources { get; }
    }
    
    /// <summary>
    /// 「ゲームっぽい移動入力」をI/F化したクラス。
    /// 何かしらの形でキーがアサインされたゲームパッドやキーボードの入力によって実装される。
    /// </summary>
    public interface IGameInputSource
    {
        bool IsActive { get; }
        IObservable<Vector2> MoveInput { get; }
        IObservable<Unit> Jump { get; }
        IObservable<bool> IsCrouching { get; }
    }
}
