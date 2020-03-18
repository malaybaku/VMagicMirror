using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 手の領域検出をするやつのInterface。
    /// 設定値系のは入れてないが、これは「ソフトウェアの仕様的にカスタムさせないでもいいのでは」と思ってるからです
    /// </summary>
    public interface IHandAreaDetector
    {
        /// <summary> 画像座標において、顔よりも左側に対する手領域の検出結果を取得します。 </summary>
        IHandDetectResult LeftSideResult { get; }
        
        /// <summary> 画像座標において、顔よりも右側に対する手領域の検出結果を取得します。 </summary>
        IHandDetectResult RightSideResult { get; }
        
        /// <summary>
        /// 取得済みの画像、サイズ、および検出された顔エリアの情報を用いて手を検出します。
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="faceRect"></param>
        void UpdateHandDetection(Color32[] colors, int width, int height, Rect faceRect);

        /// <summary>
        /// 画像は取得したが顔は検出出来なかった場合に呼び出すことで、手の検出が失敗した状態へと更新します。
        /// </summary>
        void UpdateHandDetectionWithoutFace();
    }

    /// <summary>
    /// 顔の左、または右側での手の検出結果をインターフェースとして定義します。
    /// </summary>
    public interface IHandDetectResult
    {
        /// <summary> 手の領域があると推定できたかどうか。 </summary>
        /// <remarks>
        /// VMagicMirror上の処理の特徴として、顔が取得できなかった画像を入力にすると手の検出も必ず失敗することに注意して下さい。
        /// </remarks>
        bool HasValidHandArea { get; }
        
        /// <summary> OpenCVの画像座標系ベースで、いわゆるOBB(Oriented Bounding Box)形式の手の検出エリアの中心を取得します。 </summary>
        Vector2 HandAreaCenter { get; }

        /// <summary> OpenCVの画像座標系ベースで、いわゆるOBB(Oriented Bounding Box)形式の手の検出エリアのサイズを取得します。 </summary>
        Vector2 HandAreaSize { get; }

        /// <summary> OpenCVの画像座標系ベースで、いわゆるOBB(Oriented Bounding Box)形式の手の検出エリアの回転角を取得します。 </summary>
        float HandAreaRotation { get; }

        /// <summary> OpenCVの画像座標系ベースで、指のあいだを検出したとき指の間から指先方向に向かうベクトルを取得します。 </summary>
        /// <remarks>
        /// Countを参照すると指が何本立っているか推定できます(0: グー, 1,2: チョキ, 3以上: パー)。
        /// ベクトルを平均すると、理想的には手の上向き方向が推定できます(これはHandAreaの向きと近い値になるはずです)。
        /// </remarks>
        List<Vector2> ConvexDefectVectors { get; }
    }
}

