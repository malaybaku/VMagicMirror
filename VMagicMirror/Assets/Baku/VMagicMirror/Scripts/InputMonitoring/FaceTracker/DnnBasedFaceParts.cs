using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// OpenCVforUnityのdnnベースの顔検出を前提にした、顔パーツの配置からの計算処理
    /// 画像座標は左下隅を(0, 0)とすること。
    /// デフォルトでは左右反転がオンで、DisableHorizontalFlipがtrueになるとCalculateの呼び出し結果で左右非反転の値が入ります。
    /// </summary>
    public class DnnBasedFaceParts 
    {
        //NOTE: 呼び出し元のほうで、dnnの結果をこのプロパティ群に突っ込んでいく
        //TODO: 座標の取り決め必要。入力時点で左下(0,0)を強制するのが無難かな？実装はこの座標を前提に組みます。
        
        /// <summary> 画像全体のサイズ </summary>
        public Vector2 ImageSize { get; set; }
        
        /// <summary> 顔が映っていたエリア。矩形で取るので回転は考慮しない </summary>
        public Rect FaceArea { get; set; }
        
        /// <summary> 左目の位置。開閉とは関係なく、眼窩の中心を推定 </summary>
        public Vector2 LeftEye { get; set; }
        
        /// <summary> 右目の位置。開閉とは関係なく、眼窩の中心を推定 </summary>
        public Vector2 RightEye { get; set; }
        
        /// <summary> 口の左端の位置。開閉とか、口自体が左右に動いたとかは割と無関係な位置が推定されます </summary>
        public Vector2 MouthLeft { get; set; }
        
        /// <summary> 口の右端の位置。開閉とか、口自体が左右に動いたとかは割と無関係な位置が推定されます </summary>
        public Vector2 MouthRight { get; set; }

        /// <summary> 鼻先の位置。ほかの目、口のランドマーク(4点)と鼻先の位置を比べるとヨー、ピッチが推定できます </summary>
        public Vector2 NoseTop { get; set; }
        
        /// <summary> trueを指定した場合、左右反転を無効にします。 </summary>
        public bool DisableHorizontalFlip { get; set; }
        
        //NOTE: ヨー、ピッチは純粋な幾何的角度が出ない(鼻の高さにやや依存する+ヒューリスティック計算が胡散臭い)。
        //そのため、だいたい[-1, 1]程度の幅に収まるようなファクターとして扱う
        
        /// <summary> 中央がゼロ、右向きが正のヨー回転のファクタを-1, 1の範囲の値として取得します。 </summary>
        public float YawFactor { get; private set; }
        
        /// <summary> 中央がゼロ、うつむく方向が正のピッチ回転ファクタを-1, 1の範囲の値として取得します。 </summary>
        public float PitchFactor { get; private set; }
        
        /// <summary> 顔を左にかしげる方向を正とする回転角度を単位degで取得します。 </summary>
        public float RollAngleDeg { get; private set; }
        
        //NOTE: x,yは画面内の位置に対する顔中心の位置を[-0.5, 0.5]x[-0.5, 0.5]の範囲で表したもの。
        //zは距離ファクターのつもりだけど、とりあえず常時0を出します。なぜならDNNの顔トラだとzがいまいち信用できなさそうなので…
        public Vector3 FacePosition { get; private set; }
        public Vector2 FaceXyPosition => new Vector2(FacePosition.x, FacePosition.y);

        //TODO: 検証段階ではキャリブレーションを無視します。
        //っていうか低出力のケースとどうキャリブ使い分けるんだコレ。共通にする？別々にキャリブ？

        /// <summary>
        /// 現在設定されている顔全体、および顔パーツの位置に基づいてトラッキング情報を再計算します。
        /// </summary>
        public void Calculate()
        {
            //x座標、ヨー、ロールに効く。yとかピッチは関係ないので無効。
            float flipFactor = DisableHorizontalFlip ? 1 : -1;

            //おおまかなアプローチ
            //顔の位置: 深く考えずに返しとく。
            //ロール: 目の中心-口の中心に線を引いて角度を取るとだいたい正しい値となる。
            //ヨー: 鼻先が右の目-口のラインに近いか、左の目-口ラインに近いかで判別
            //ピッチ: 鼻先が目の中心と口の中心のどっち寄りかで判別(目に近ければ上向き)

            //ヨー、ピッチは上記のヒューリスティックが大体pnp問題を解くのと等価になるため、
            //pnpを解くのをサボってしまおう、というアプローチです。
            
            //NOTE: 二重に面倒で申し訳ないのだけど、ImageBasedBodyMotionとFaceTrackerの挙動がアレなのでxは左右を逆にします…うーん…
            FacePosition = new Vector3(
                -flipFactor * (FaceArea.center.x / ImageSize.x - 0.5f),
                FaceArea.center.y / ImageSize.y - 0.5f, 
                0
                );
            
            //ロール: 方向だけでいいのでスケールを省いている。0.5倍すると実際の画像上のベクトル
            var rollLine = ((LeftEye + RightEye) - (MouthLeft + MouthRight)).normalized;
            RollAngleDeg = flipFactor * Mathf.Atan2(rollLine.x, rollLine.y) * Mathf.Rad2Deg;

            YawFactor = flipFactor * CalculateYaw();
            PitchFactor =  CalculatePitch();
        }

        private float CalculateYaw()
        {
            //右目-口の右をつないだ直線と鼻先との距離を測る。左も同様。
            
            //NOTE: 基準になる直線のベクトル(目-口の直線)をnormalizeすることで、ベクトルの外積 = 直線に対する点の距離となる
            //距離と言いつつ正負の概念があるので注意: また外積の計算の符号にも注意
            var rightN = NoseTop - RightEye;
            var rightBase = (MouthRight - RightEye).normalized;
            //負だったら「鼻が右に向きすぎて凸包の外に出てる」みたいな意味になる。
            var rightLength = -(rightN.x * rightBase.y - rightN.y * rightBase.x);

            var leftN = NoseTop - LeftEye;
            var leftBase = (MouthLeft - LeftEye).normalized;
            //向きの関係で符号が反転することに注意。こっちが負の場合、鼻がめっちゃ左に向いている。
            var leftLength = leftN.x * leftBase.y - leftN.y * leftBase.x;
            
            return CalculateRateByLength(leftLength, rightLength);
        }

        private float CalculatePitch()
        {
            //計算コンセプトはヨーと一緒。正負に注意しなきゃいけないのもヨーと一緒。
            var eyeBase = (LeftEye - RightEye).normalized;
            var rightEyeToNose = NoseTop - RightEye;
            var eyeLineToNoseLength = rightEyeToNose.x * eyeBase.y - rightEyeToNose.y * eyeBase.x;

            var mouthBase = (MouthRight - MouthLeft).normalized;
            var mouthLeftToNose = NoseTop - MouthLeft;
            var mouthLineToNoseLength = mouthLeftToNose.x * mouthBase.y - mouthLeftToNose.y * mouthBase.x;

            return CalculateRateByLength(eyeLineToNoseLength, mouthLineToNoseLength);
        }

        //負の場合も有り得るような距離を2つ渡すことで、内分の比のような値を計算します。
        private static float CalculateRateByLength(float negativeLen, float positiveLen)
        {
            //通常ありえないけど一応…
            if (negativeLen < 0 && positiveLen < 0)
            {
                return 0f;
            }
            
            if (negativeLen < 0)
            {
                return -1f;
            }

            if (positiveLen < 0)
            {
                return 1f;
            }

            //negative側の距離が長い = positive側に近い位置にいる = 比率としては1寄り、みたいな話
            var rate = negativeLen / (negativeLen + positiveLen);
            return rate * 2f - 1f;
        }
    }
}
