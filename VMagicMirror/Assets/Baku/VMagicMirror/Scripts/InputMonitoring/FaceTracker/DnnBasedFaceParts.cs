using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// OpenCVforUnityのdnnベースの顔検出を前提にした、顔パーツの配置からの計算処理
    /// 画像座標は左下隅を(0, 0)とすること。
    /// デフォルトでは左右反転がオンで、DisableHorizontalFlipがtrueになるとCalculateの呼び出し結果で左右非反転の値が入ります。
    /// </summary>
    public class DnnBasedFaceParts  : IFaceAnalyzeResult
    {
        //NOTE: 呼び出し元のほうで、dnnの結果をこのプロパティ群に突っ込んでいく
        //TODO: 座標の取り決め必要。入力時点で左下(0,0)を強制するのが無難かな？実装はこの座標を前提に組みます。
        
        #region interfaceじゃないけど必要なデータ入力

        /// <summary> 画像全体のサイズ </summary>
        public Vector2 ImageSize { get; set; }
        
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
        
        #endregion
   
        /// <summary> trueを指定した場合、左右反転を無効にします。 </summary>
        public bool DisableHorizontalFlip { get; set; }
        
        private Rect _faceRect;

        /// <summary> 顔が映っていたエリア。矩形で取るので回転は考慮しない </summary>
        public Rect FaceRect
        {
            get => _faceRect;
            set
            {
                _faceRect = value;
                //1回でも明示的に入ってればとりあえずOK、とする。わりとラフだけど。
                HasFaceRect = true;
            }
        }

        public bool HasFaceRect { get; private set; }

        private Vector2 _facePosition;
        public Vector2 FacePosition =>
            DisableHorizontalFlip ? new Vector2(-_facePosition.x, _facePosition.y) : _facePosition;


        public float PitchRate { get; private set; }
        
        private float _yawRate;
        public float YawRate => DisableHorizontalFlip ? -_yawRate : _yawRate;

        private float _rollRad;
        public float RollRad => DisableHorizontalFlip ? -_rollRad : _rollRad;

        //NOTE: 低出力と同じでFaceRectのサイズを基準にしてもいいかもしれない
        public float ZOffset { get; private set; }
        
        //とりあえずまばたきは無視。局所的な画像分析も出来そうではあるけど…。
        public bool CanAnalyzeBlink => false;
        public float LeftBlink => 0f;
        public float RightBlink => 0f;
        
        /// <summary>
        /// 現在設定されている顔全体、および顔パーツの位置に基づいてトラッキング情報を再計算します。
        /// </summary>
        public void Calculate(CalibrationData calibration, bool shouldCalibrate)
        {
            HasFaceRect = true;
            
            //おおまかなアプローチ
            //顔の位置: 深く考えずに中心で取る
            //ロール: 目の中心-口の中心に線を引いて角度を取るとだいたい正しい値となる。
            //ヨー: 鼻先が右の目-口のラインに近いか、左の目-口ラインに近いかで判別
            //ピッチ: 鼻先が目の中心と口の中心のどっち寄りかで判別(目に近ければ上向き)

            //ヨー、ピッチは上記のヒューリスティックが大体pnp問題を解くのと等価になるため、
            //pnpを解くのをサボってしまおう、というアプローチです。
            
            _facePosition = FaceRect.center - calibration.faceCenter;
            
            //ロール: 方向だけでいいのでスケールを省いている。0.5倍すると実際の画像上のベクトル
            var rollLine = ((LeftEye + RightEye) - (MouthLeft + MouthRight)).normalized;
            _rollRad = -Mathf.Atan2(rollLine.x, rollLine.y);

            _yawRate = -CalculateYaw();
            PitchRate = CalculatePitch() - calibration.dnnPitchRateOffset;
            
            //NOTE: 値をすぐ0に戻すことで、キャリブレーションした値ベースで計算したように扱う
            if (shouldCalibrate)
            {
                calibration.faceSize = FaceRect.width * FaceRect.height;
                calibration.faceCenter = FaceRect.center;
                calibration.dnnPitchRateOffset = PitchRate;
                _facePosition = Vector2.zero;
                PitchRate = 0f;
            }
        }

        public void LerpToDefault(float lerpFactor)
        {
            var factor = 1.0f - lerpFactor;

            PitchRate *= factor;
            _yawRate *= factor;
            _rollRad *= factor;
            ZOffset *= factor;
            _facePosition *= factor;
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
