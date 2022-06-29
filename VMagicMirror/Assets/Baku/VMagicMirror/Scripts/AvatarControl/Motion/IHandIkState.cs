using System;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 状態の1つとして手のIKを管理するようなインターフェースです。
    /// <see cref="HandIKIntegrator"/>からステートっぽく呼び出すために使います。
    /// </summary>
    public interface IHandIkState : IIKData
    {
        /// <summary>
        /// 他のステートからこのステートに切り替わったときにIKのブレンド処理をスキップしてよいかどうか。
        /// 普通はfalseで、直前ステートのIK位置からの軌道を自力で計算するステートでのみtrueにする
        /// </summary>
        bool SkipEnterIkBlend { get; }

        /// <summary>
        /// このデータが左手のものか右手のものかを取得します。
        /// </summary>
        ReactedHand Hand { get; }
        
        /// <summary>
        /// このデータが何用のIKデータを生成しているのかを取得します。
        /// </summary>
        HandTargetType TargetType { get; }

        /// <summary>
        /// このStateを使ってほしい、というとき、このinterface自身を引数にして発火します。
        /// あくまで発火するだけで、購読側が無視しても構いません。
        /// </summary>
        event Action<IHandIkState> RequestToUse;

        /// <summary>
        /// このIKを適用開始するとき、直前のIKの種類を指定して呼ばれます。
        /// </summary>
        void Enter(IHandIkState prevState);

        /// <summary>
        /// このIKを適用終了するとき、次のIKの種類を指定して呼ばれます。
        /// </summary>
        void Quit(IHandIkState nextState);
    }
}
