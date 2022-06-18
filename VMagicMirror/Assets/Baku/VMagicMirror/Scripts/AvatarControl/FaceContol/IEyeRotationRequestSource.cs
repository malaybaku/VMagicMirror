using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 無次元化した目の回転値を(-1, 1)の範囲でリクエストするクラスが実装するI/F。
    /// </summary>
    /// <remarks>
    /// xは右が正、yは上が正。-1,1は限界まで左右/上下に目を動かした状態を表す。
    /// </remarks>
    public interface IEyeRotationRequestSource
    {
        public bool IsActive { get; }
        
        public Vector2 LeftEyeRotationRate { get; }
        public Vector2 RightEyeRotationRate { get; }
    }
}
