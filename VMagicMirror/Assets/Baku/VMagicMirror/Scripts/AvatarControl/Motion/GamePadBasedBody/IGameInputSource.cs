using System;
using R3;
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
        //NOTE: I/Fの実装ではデフォルトが歩きか走りか考慮しないでOKで、切り替え指示が出てる…ということのみを通知する
        IObservable<bool> IsRunWalkToggleActive { get; }
        IObservable<bool> GunFire { get; }

        IObservable<Unit> Jump { get; }
        IObservable<Unit> Punch { get; }
        
        /// <summary> .vrma のカスタムモーションはココからkeyを指定して発火 </summary>
        IObservable<string> StartCustomMotion { get; }
        /// <summary>
        /// .vrma のカスタムモーションが割当たっているボタン/キーを離すことで発火する。
        /// ただし、この発火でモーションが止まるのはループモーションのみ
        /// </summary>
        IObservable<string> StopCustomMotion { get; }
    }
}
