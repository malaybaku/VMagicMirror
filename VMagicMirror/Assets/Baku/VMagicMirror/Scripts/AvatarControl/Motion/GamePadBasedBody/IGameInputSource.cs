using System;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.GameInput
{
    public interface IGameInputSourceSwitcher
    {
        public IReadOnlyReactiveProperty<bool> IsActive { get; }
        public IReadOnlyReactiveProperty<IGameInputSource> Source { get; }
    }
    
    /// <summary>
    /// 「ゲームっぽい移動入力」をI/F化したクラス。
    /// 何かしらの形でキーがアサインされたゲームパッドやキーボードの入力によって実装される。
    /// </summary>
    public interface IGameInputSource
    {
        bool IsActive { get; }
        Vector2 MoveInput { get; }
        IObservable<Unit> Jump { get; }
        bool IsCrouching { get; }
    }

    /// <summary>
    /// 「ゲーム入力を使わない」という条件下で使うやつ
    /// </summary>
    public class EmptyGameInputSource : IGameInputSource
    {
        bool IGameInputSource.IsActive => false;
        Vector2 IGameInputSource.MoveInput => Vector2.zero;
        IObservable<Unit> IGameInputSource.Jump => Observable.Empty<Unit>();
        bool IGameInputSource.IsCrouching => false;

        public static EmptyGameInputSource Instance { get; } = new EmptyGameInputSource();
    }
}
