using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 17点ランドマークの顔情報をもとに目の開閉と顔の向きを推定するクラス。
    /// いわゆるPnP(Perspective n-Point)をやらず、カンタンな計算で済ませる事を狙っている
    /// </summary>
    public class FaceParts
    {
        //顔のヨー角を輪郭と口の中心のキョリの非で求める際に用いる比率。
        //値が大きいほど、ヨー角が小さくなる。
        private const float YawMouthDistanceRatio = 1.0f;        
        //EyeFaceYDiffがとる値の限界値の目安。この値で割って正規化する
        private const float EyeFaceYDiffMax = 0.3f;

        private const int FaceLandmarkCount = 17;

        private readonly Vector2[] _landmarks = new Vector2[FaceLandmarkCount];

        /// <summary> FaceRollRadの符号に影響する。 </summary>
        public bool DisableHorizontalFlip { get; set; }
        
        #region 出力値いろいろ

        public float FacePitchRate { get; private set; }
        public float FaceYawRate => DisableHorizontalFlip ? -_faceYawRate : _faceYawRate;
        public float FaceRollRad => DisableHorizontalFlip ? -_faceRollRad : _faceRollRad;

        public float LeftEyeBlink => DisableHorizontalFlip ? _leftEyeBlink : _rightEyeBlink;
        public float RightEyeBlink => DisableHorizontalFlip ? _rightEyeBlink : _leftEyeBlink;

        private float _faceRollRad;
        private float _faceYawRate;
        private float _leftEyeBlink;
        private float _rightEyeBlink;
        
        //NOTE: 1回のアップデートあたり4回くらい使いたくなるのでキャッシュ
        private float _faceRollCos;
        private float _faceRollSin;
        
        #endregion
        
        public void Update(Rect mainPersonRect, List<Vector2> landmarks)
        {
            for (int i = 0; i < FaceLandmarkCount; i++)
            {
                _landmarks[i] = landmarks[i];
            }
            
            UpdateEyeOpen();
            UpdateFaceRotation();
        }
    
        public void LerpToDefault(CalibrationData calibration, float lerpFactor)
        {
            var factor = 1.0f - lerpFactor;
            _leftEyeBlink *= factor;
            _rightEyeBlink *= factor;
            
            //TODO: キャリブレーション次第で変えないとダメ。とくにピッチ
            
            FacePitchRate *= factor;
            _faceYawRate *= factor;
            _faceRollRad *= factor;
            
            // Outline.LerpToDefault(calibration, lerpFactor);
            // for (int i = 0; i < PartsWithoutOutline.Length; i++)
            // {
            //     PartsWithoutOutline[i].LerpToDefault(calibration, lerpFactor);
            // }
        }
        

        private void UpdateEyeOpen()
        {
            float leftEyeHeight = new Vector2 (_landmarks [12].x - _landmarks [11].x, _landmarks [12].y - _landmarks [11].y).sqrMagnitude;
            float rightEyeHeight = new Vector2 (_landmarks [10].x - _landmarks [9].x, _landmarks [10].y - _landmarks [9].y).sqrMagnitude;
            float noseHeight = new Vector2 (_landmarks [1].x - (_landmarks [3].x + _landmarks [4].x) / 2, _landmarks [1].y - (_landmarks [3].y + _landmarks [4].y) / 2).sqrMagnitude;

            float leftEyeOpenRatio = leftEyeHeight / noseHeight;
            _leftEyeBlink = 1.0f - Mathf.InverseLerp (0.003f, 0.009f, leftEyeOpenRatio);

            float rightEyeOpenRatio = rightEyeHeight / noseHeight;
            _rightEyeBlink = 1.0f - Mathf.InverseLerp (0.003f, 0.009f, rightEyeOpenRatio);
        }

        private void UpdateFaceRotation()
        {
            UpdateRoll();
            UpdateYaw();
            //NOTE: Rollより後にやる必要があるので注意
            UpdatePitch();
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

        private void UpdatePitch()
        {
            //顔のY平均と、目のY平均の上下関係から推定する。
            //目のほうが上にある場合は上をむいており、逆もしかり。
            
            var leftEyeCenter = 0.25f * (_landmarks[4] + _landmarks[5] + _landmarks[11] + _landmarks[12]);
            var rightEyeCenter = 0.25f * (_landmarks[2] + _landmarks[3] + _landmarks[9] + _landmarks[10]);
            
            var faceY = 0.5f * GetRollCanceled(_landmarks[6] + _landmarks[8]).y;
            var eyesY = 0.5f * GetRollCanceled(leftEyeCenter + rightEyeCenter).y;
                
            //POINT: 顔の横方向サイズで正規化する。こうすると口の開閉とか鎖骨付近の領域の誤検出にちょっと強いので
            FacePitchRate = (eyesY - faceY) / (_landmarks[6] - _landmarks[8]).magnitude / EyeFaceYDiffMax;
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
    }
}
