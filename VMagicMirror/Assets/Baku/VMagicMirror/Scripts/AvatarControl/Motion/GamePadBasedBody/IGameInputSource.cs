using System;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.GameInput
{
    /// <summary>
    /// 「ゲームっぽい入力」をI/F化したクラス。
    /// 何かしらの形でキーがアサインされたゲームパッドやキーボードの入力によって実装される。
    /// </summary>
    public interface IGameInputSource
    {
        //NOTE: 下記はRP<T>でいい気もするが、複数デバイスからのデータの後勝ち…というのの表現としてIO<T>が良さそうなのでそうしている
        // - Vector2の値はいずれもx,yがそれぞれ[-1, 1]の範囲に収まる事が必要。
        // - magnitudeは1を超えてもOKで、例えば(1, 1)のような値になってもよい
        IObservable<Vector2> MoveInput { get; }
        /// <summary>
        /// ゼロ入力 = 正面向き
        /// +x = 右
        /// +y = 上
        /// 首をかしげる動きは無し
        /// </summary>
        IObservable<Vector2> LookAroundInput { get; }
        IObservable<bool> IsCrouching { get; }
        IObservable<bool> IsRunning { get; }

        IObservable<Unit> Jump { get; }
        IObservable<Unit> Punch { get; }
        IObservable<Unit> GunTrigger { get; }
    }
}
