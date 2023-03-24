using System;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.GameInput
{
    /// <summary>
    /// 「ゲームっぽい移動入力」をI/F化したクラス。
    /// 何かしらの形でキーがアサインされたゲームパッドやキーボードの入力によって実装される。
    /// </summary>
    public interface IGameInputSource
    {
        bool IsActive { get; }

        //NOTE: 下記はRP<T>でいい気もするが、複数デバイスからのデータの後勝ち…というのの表現としてIO<T>が良さそうなのでそうしている
        IObservable<Vector2> MoveInput { get; }
        /// <summary> ゼロ入力は正面向きを表し、非ゼロ入力があると右や左を見ようとする動きとして解釈される </summary>
        IObservable<Vector2> LookAroundInput { get; }
        IObservable<bool> IsCrouching { get; }
        IObservable<bool> IsRunning { get; }

        IObservable<Unit> Jump { get; }
        IObservable<Unit> Punch { get; }
        IObservable<Unit> GunTrigger { get; }
    }
}
