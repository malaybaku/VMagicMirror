using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 17点ランドマークの顔情報をもとに目の開閉と顔の向きを推定するクラス。
    /// いわゆるPnP(Perspective n-Point)をやらず、カンタンな計算で済ませる事を狙っている
    /// </summary>
    public class FaceParts : IFaceAnalyzeResult
    {
        //顔のヨー角を輪郭と口の中心のキョリの非で求める際に用いる比率。
        //値が大きいほど、ヨー角が小さくなる。
        private const float YawMouthDistanceRatio = 1.0f;        
        //EyeFaceYDiffがとる値の限界値の目安。この値で割って正規化する
        private const float EyeFaceYDiffMax = 0.3f;

        private const int FaceLandmarkCount = 17;

        private readonly Vector2[] _landmarks = new Vector2[FaceLandmarkCount];
        
        /// <summary>
        /// NOTE: このフラグはヨー、ピッチ、x方向の位置、(使う場合は)まばたき値に影響します。
        /// </summary>
        public bool DisableHorizontalFlip { get; set; }
        
        #region 出力値いろいろ

        public bool HasFaceRect { get; private set; }
        
        public Rect FaceRect { get; private set; }

        public Vector2 FacePosition => 
            DisableHorizontalFlip ? new Vector2(-_facePosition.x, _facePosition.y) : _facePosition; 

        public float ZOffset { get; private set; }
        
        public float PitchRate { get; private set; }
        public float YawRate => DisableHorizontalFlip ? -_faceYawRate : _faceYawRate;
        public float RollRad => DisableHorizontalFlip ? -_faceRollRad : _faceRollRad;

        public bool CanAnalyzeBlink => true;
        public float LeftBlink => DisableHorizontalFlip ? _leftBlink : _rightBlink;
        public float RightBlink => DisableHorizontalFlip ? _rightBlink : _leftBlink;

        //TODO: 内部的にはrawな計算値も持ってないとキャリブレーション指示が来たとき困るかも
        private Vector2 _facePosition;
        private float _faceRollRad;
        private float _faceYawRate;
        //NOTE: 1回のアップデートあたり4回くらい使いたくなるのでキャッシュ
        private float _faceRollCos;
        private float _faceRollSin;

        private float _leftBlink;
        private float _rightBlink;
        
        #endregion
        
        public void Update(Rect faceRect, List<Vector2> landmarks, CalibrationData calibration, bool shouldCalibrate)
        {
            //前半は位置の計算、後半は角度の計算。
            FaceRect = faceRect;
            HasFaceRect = true;            
            
            _facePosition = faceRect.center - calibration.faceCenter;
            //NOTE: zは特徴点を使って推定した方がよいかも
            float faceSizeFactor = (faceRect.width * faceRect.height) / calibration.faceSize;
            ZOffset = FaceSizeFactorToZ(faceSizeFactor);
            
            for (int i = 0; i < FaceLandmarkCount; i++)
            {
                _landmarks[i] = landmarks[i];
            }
            UpdateRoll();
            UpdateYaw();
            //NOTE: Rollより後にやる必要があるので注意
            UpdatePitch(calibration);
            UpdateBlinkValues(landmarks);
            
            //NOTE: 値をすぐ0に戻すことで、キャリブレーションした値ベースで計算したように扱う
            if (shouldCalibrate)
            {
                calibration.faceSize = faceRect.width * faceRect.height;
                calibration.faceCenter = faceRect.center;
                calibration.pitchRateOffset = PitchRate;
                _facePosition = Vector2.zero;
                PitchRate = 0f;
            }
        }
    
        public void LerpToDefault(float lerpFactor)
        {
            var factor = 1.0f - lerpFactor;
            
            PitchRate *= factor;
            _faceYawRate *= factor;
            _faceRollRad *= factor;

            _leftBlink *= 1.0f - lerpFactor;
            _rightBlink *= 1.0f - lerpFactor;

            ZOffset *= factor;
            _facePosition *= factor;
            
            //NOTE: rectは変更しないでよいことに注意
        }
    
        private void UpdateRoll()
        {
            //輪郭の端、つまり両こめかみ付近に線を引いてみたときの傾きをとっている。
            //以前はアゴ先の位置も考慮していたが、それだとヨー運動と合成されてしまうため、使わないようにした。
            Vector2 diffVecSum = _landmarks[8] - _landmarks[6];
            _faceRollRad = -Mathf.Atan2(diffVecSum.y, diffVecSum.x);
            
            //NOTE: ↑がマイナス入れてる関係で↓の符号もちょっと怪しい？
            _faceRollCos = Mathf.Cos(_faceRollRad);
            _faceRollSin = Mathf.Sin(_faceRollRad);
        }

        private void UpdateYaw()
        {
            var mouthCenter = (_landmarks[13] + _landmarks[14] + _landmarks[15] + _landmarks[16]) * 0.25f;
            float diffLeft = Vector2.Distance(mouthCenter, _landmarks[6]);
            float diffRight = Vector2.Distance(mouthCenter, _landmarks[8]);
            //ピクセル単位のハズなので1以下ならどちらかの点に被っている(※通常は起きない)
            //通常ケースでは(遠いほうの距離 / 近いほうの距離)の比率をうまく畳んで[-1, 1]の範囲に収めようとしている
            _faceYawRate =
                (diffLeft < 1f) ? -1f :
                (diffRight < 1f) ? 1f :
                (diffLeft < diffRight) ? 
                    -Mathf.Clamp(diffRight / diffLeft - 1, 0, YawMouthDistanceRatio) / YawMouthDistanceRatio :
                    Mathf.Clamp(diffLeft / diffRight - 1, 0, YawMouthDistanceRatio) / YawMouthDistanceRatio;
        }

        private void UpdatePitch(CalibrationData calibration)
        {
            //顔のY平均と、目のY平均の上下関係から推定する。
            //目のほうが上にある場合は上をむいており、逆もしかり。
            
            var leftEyeCenter = 0.25f * (_landmarks[4] + _landmarks[5] + _landmarks[11] + _landmarks[12]);
            var rightEyeCenter = 0.25f * (_landmarks[2] + _landmarks[3] + _landmarks[9] + _landmarks[10]);
            
            var faceY = 0.5f * GetRollCanceled(_landmarks[6] + _landmarks[8]).y;
            var eyesY = 0.5f * GetRollCanceled(leftEyeCenter + rightEyeCenter).y;
                
            //POINT: 顔の横方向サイズで正規化する。こうすると口の開閉とか鎖骨付近の領域の誤検出にちょっと強いので
            float rawPitchRate = (eyesY - faceY) / (_landmarks[6] - _landmarks[8]).magnitude / EyeFaceYDiffMax;
            PitchRate = rawPitchRate - calibration.pitchRateOffset;
        }
            
        private Vector2 GetRollCanceled(Vector2 p)
        {
            //sinにマイナスがつくのは、キャンセル回転のためにもとの角度を(-1)倍した値のsin,cosをセットで得るため
            float cos =  _faceRollCos;
            float sin = -_faceRollSin;
            return new Vector2(
                p.x * cos - p.y * sin,
                p.x * sin + p.y * cos
                );
        }

        private void UpdateBlinkValues(List<Vector2> points)
        {
            //NOTE: 点を貰った瞬間に目の開閉を計算し終えてしまう
            var leftEyeHeight = new Vector2 (points [12].x - points [11].x, points [12].y - points [11].y).sqrMagnitude;
            var rightEyeHeight = new Vector2 (points [10].x - points [9].x, points [10].y - points [9].y).sqrMagnitude;
            var noseHeight = new Vector2 (points [1].x - (points [3].x + points [4].x) / 2, points [1].y - (points [3].y + points [4].y) / 2).sqrMagnitude;

            float leftEyeOpenRatio = leftEyeHeight / noseHeight;
            _leftBlink = 1.0f - Mathf.InverseLerp (0.003f, 0.009f, leftEyeOpenRatio);

            float rightEyeOpenRatio = rightEyeHeight / noseHeight;
            _rightBlink = 1.0f - Mathf.InverseLerp (0.003f, 0.009f, rightEyeOpenRatio);
        }

        //NOTE: ここ適当。たとえばSqrtしたり、逆にPowしたりすることも考えられるし、
        //そもそも顔rectのサイズよりよい方法があればそっちに乗り換えてほしい
        private static float FaceSizeFactorToZ(float v) => -(v - 1) * 0.1f;

    }
}
