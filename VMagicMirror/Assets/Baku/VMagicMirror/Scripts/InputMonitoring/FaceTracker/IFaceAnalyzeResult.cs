using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 顔トラッキングの結果を表すデータ。
    /// この出力の時点で左右の反転や、キャリブレーションによるオフセット除去まで行ったあとのデータが出てきます。
    /// </summary>
    /// <remarks>
    /// Yaw, Pitchを無次元量としているのは基本的にpnp問題をマジメに解かない形でしか顔トラッキングしてないため。
    /// Rollは相当いい加減にやっても計算可能であるため、角度が来る想定にしている。
    /// </remarks>
    public interface IFaceAnalyzeResult
    {
        /// <summary>
        /// 指定した値を元に、徐々に姿勢を原点位置に戻します。
        /// </summary>
        /// <param name="lerpFactor"></param>
        void LerpToDefault(float lerpFactor);
        
        /// <summary> 1回以上<see cref="FaceRect"/>に有効な値が書き込まれるとtrueになるフラグを取得します。 </summary>
        bool HasFaceRect { get; }
        
        //TODO: このI/FとしてはFaceRect要らないのでは？ハンドトラッキングとかで欲しいのは分かるけども。
        /// <summary>
        /// 顔の検出領域を、x方向の画像座標が[-0.5, 0.5]であるように正規化したものを取得します。
        /// この値は他と異なり、キャリブレーションする前の、画像に関する値が入っています。
        /// </summary>
        Rect FaceRect { get; }
        
        /// <summary> 顔の中心位置を、<see cref="FaceRect"/>と同じスケールで正規化したものを取得します。 </summary>
        /// <remarks>
        /// ふつうFaceRect.Centerを使って実装可能です。
        /// </remarks>
        Vector2 FacePosition { get; }

        /// <summary>
        /// 顔のZ座標が基準位置よりもwebカメラに近ければマイナス、遠ければプラスの位置になるような値を取得します。
        /// Z座標を考慮するオプションが無効な場合は計算をしないでも構いません。
        /// </summary>
        float ZOffset { get; }
        
        /// <summary> ヨー角度の度合いを、右向きを正とし、およそ[-1, 1]に正規化された値として取得します。 </summary>
        float YawRate { get; }

        /// <summary> ピッチ角の度合いを、うつむきを正とし、およそ[-1, 1]に正規化された値として取得します。 </summary>
        float PitchRate { get; }

        /// <summary> ロール角を、左に首をかしげる方向を正とし、単位[rad]で取得します。 </summary>
        float RollRad { get; }
        
        
        /// <summary>
        /// 顔トラッキングの処理でまばたき値が計算可能かどうかを取得します。
        /// この値がtrueの場合、かつまばたき画像処理が有効の場合、LeftBlinkやRightBlinkに有効な値を入れます。
        /// </summary>
        bool CanAnalyzeBlink { get; }
     
        /// <summary> <see cref="CanAnalyzeBlink"/>がtrueの場合、左目のまばたき具合を取得します。</summary>
        float LeftBlink { get; }
        
        /// <summary> <see cref="CanAnalyzeBlink"/>がtrueの場合、右目のまばたき具合を取得します。</summary>
        float RightBlink { get; }
    }
}
