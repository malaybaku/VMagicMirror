using System;
using System.Collections.Generic;
using UnityEngine;

namespace Baku.VMagicMirror
{
    //NOTE: いったん簡単な実装で載せたいので、番号付けをいい加減にして統計情報チックな扱いに留める。

    /// <summary>
    /// 68点ランドマークの顔情報をもとに顔の個別パーツの情報を更新するクラス
    /// </summary>
    public class FaceParts
    {
        //顔のヨー角を輪郭と口の中心のキョリの非で求める際に用いる比率。
        //値が大きいほど、ヨー角が小さくなる。
        private const float YawMouthDistanceRatio = 1.0f;        
        //EyeFaceYDiffがとる値の限界値の目安。この値で割って正規化する
        private const float EyeFaceYDiffMax = 0.12f;

        
        public const int FaceLandmarkCount = 17;

        private readonly Vector2[] _landmarks = new Vector2[FaceLandmarkCount];

        // public FaceParts()
        // {
        //     Outline = new FaceOutlinePart(this);
        //
        //     Nose = new NosePart(this);
        //
        //     RightEye = new RightEyePart(this, Nose);
        //     LeftEye = new LeftEyePart(this, Nose);
        //
        //     Mouth = new MouthPart(this);
        //
        //     PartsWithoutOutline = new FacePartBase[]
        //     {
        //         Nose,
        //         RightEye,
        //         LeftEye,
        //         Mouth,
        //     };
        // }
        
        // public Vector2 FaceSize { get; private set; } = Vector2.one;

        // public FaceOutlinePart Outline { get; }
        //
        // public NosePart Nose { get; }
        //
        // public RightEyePart RightEye { get; }
        // public LeftEyePart LeftEye { get; }
        //
        // public MouthPart Mouth { get; }
        //
        // //輪郭は顔サイズと傾き除去のために特別扱いしたいので外す
        // private FacePartBase[] PartsWithoutOutline { get; }

        /// <summary> FaceRollRadの符号に影響する。 </summary>
        public bool DisableHorizontalFlip { get; set; }
        
        #region 出力値いろいろ

        public float FacePitchRate { get; private set; }
        public float FaceYawRate => DisableHorizontalFlip ? -_faceYawRate : _faceYawRate;
        public float FaceRollRad => DisableHorizontalFlip ? _faceRollRad : _faceRollRad;

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
            //TODO: 逆だったら直してね
            _faceRollRad = Mathf.Atan2(diffVecSum.y, diffVecSum.x);
        }

        private void UpdateYaw()
        {
            var mouthCenter = (_landmarks[13] + _landmarks[14] + _landmarks[15] + _landmarks[16]) * 0.25f;
            float diffLeft = Vector2.Distance(mouthCenter, _landmarks[8]);
            float diffRight = Vector2.Distance(mouthCenter, _landmarks[6]);
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
        
        // /// <summary>
        // /// 値を更新していく。
        // /// blendRateが0-0.99くらいの値のときは顔トラが始まって間もないのでキャリブ
        // /// </summary>
        // /// <param name="mainPersonRect"></param>
        // /// <param name="landmarks"></param>
        // public void Update(Rect mainPersonRect, List<Vector2> landmarks)
        // {
        //     if (landmarks == null || landmarks.Count < FaceLandmarkCount)
        //     {
        //         return;
        //     }
        //
        //     //顔の中心でオフセット取る : たぶん回転の除去とかで都合がよいのでこのスタイルで行きます
        //     var offset = mainPersonRect.center;
        //     for(int i = 0; i < _landmarks.Length; i++)
        //     {
        //         _landmarks[i] = landmarks[i] - offset;
        //     }
        //
        //     Outline.Update(_landmarks);
        //     FaceSize = Outline.FaceSize;
        //     for (int i = 0; i < PartsWithoutOutline.Length; i++)
        //     {
        //         PartsWithoutOutline[i].Update(_landmarks);
        //     }
        //     Outline.UpdateYaw(Mouth.CurrentCenterPosition);
        //     Outline.UpdatePitch(LeftEye.GetCenter(), RightEye.GetCenter());
        // }
        //
        // /// <summary>
        // /// それぞれの顔パーツ情報を初期状態に戻します。
        // /// </summary>
        // /// <param name="calibration"></param>
        // /// <param name="lerpFactor">0-1の範囲で指定される、基準位置に戻すのに使うLerpファクタ</param>
        // public void LerpToDefault(CalibrationData calibration, float lerpFactor)
        // {
        //     Outline.LerpToDefault(calibration, lerpFactor);
        //     for (int i = 0; i < PartsWithoutOutline.Length; i++)
        //     {
        //         PartsWithoutOutline[i].LerpToDefault(calibration, lerpFactor);
        //     }
        // }
        
        // //todo: 内部実装をSpanでやった方がカッコ良さそう…
        // public abstract class FacePartBase
        // {
        //     protected FacePartBase(FaceParts parent)
        //     {
        //         //_positions = new Vector2[LandmarkLength];
        //         Parent = parent;
        //     }
        //
        //     public FaceParts Parent { get; }
        //
        //     /// <summary>
        //     /// WARN: GCAllocをラクに避けるためにこう書いてるが派生クラスでの書き込みはダメ！
        //     /// </summary>
        //     //public Vector2[] Positions => _positions;
        //
        //     public abstract void Update(Vector2[] rects);
        //
        //     //NOTE: 要らなくなる、はず。
        //     protected Vector2 GetRollCanceled(Vector2 p)
        //     {
        //         //sinにマイナスがつくのは、キャンセル回転のためにもとの角度を(-1)倍した値のsin,cosをセットで得るため
        //         float cos = Parent.Outline.CurrentFaceRollCos;
        //         float sin = -Parent.Outline.CurrentFaceRollSin;
        //         return new Vector2(
        //             p.x * cos - p.y * sin,
        //             p.x * sin + p.y * cos
        //             );
        //     }
        //
        //     /// <summary> パーツの値を基準値まで戻す。このとき、必要ならばキャリブデータを参照してもよい。 </summary>
        //     public abstract void LerpToDefault(CalibrationData calibration, float lerpFactor);
        //
        // }
        //
        // public class FaceOutlinePart : FacePartBase
        // {
        //     //顔のヨー角を輪郭と口の中心のキョリの非で求める際に用いる比率。
        //     //値が大きいほど、ヨー角が小さくなる。
        //     private const float YawMouthDistanceRatio = 3.0f;
        //
        //     //EyeFaceYDiffがとる値の限界値の目安。この値で割って正規化する
        //     private const float EyeFaceYDiffMax = 0.12f;
        //
        //     public FaceOutlinePart(FaceParts parent) : base(parent)
        //     {
        //     }
        //
        //     public Vector2 FaceSize { get; private set; } = Vector2.one;
        //     
        //     public float EyeFaceYDiff { get; private set; }
        //     
        //
        //     public float CurrentFaceRollRad { get; private set; }
        //     //NOTE: sin, cosは回転計算で何度も欲しいのでキャッシュしとく
        //     public float CurrentFaceRollSin { get; private set; }
        //     public float CurrentFaceRollCos { get; private set; }
        //
        //     /// <summary>
        //     /// 左右どちらを向いているかを[-1(左), 1(右)]の範囲で表すレート。
        //     /// </summary>
        //     /// <remarks>
        //     /// 計算上レートと角度は比例しないが、近似として比例扱いにしても良い。
        //     /// </remarks>
        //     public float CurrentFaceYawRate { get; private set; }
        //
        //     /// <summary>
        //     /// 上下どちらを向いてるかを[-1(下), 1(上)]前後の範囲で表すレート。
        //     /// このレイヤーでは上が正のほうが直感的だけど、ピッチ角にする時点で符号の反転が必要なことに注意
        //     /// </summary>
        //     public float CurrentFacePitchRate => EyeFaceYDiff;
        //
        //     protected override void OnUpdated()
        //     {
        //         var positions = Positions;
        //
        //         //輪郭の端、つまり両こめかみ付近に線を引いてみたときの傾きをとっている。
        //         //以前はアゴ先の位置も考慮していたが、それだとヨー運動と合成されてしまうため、使わないようにした。
        //         Vector2 diffVecSum = positions[16] - positions[0];
        //         
        //         float faceRollRad = Mathf.Atan2(diffVecSum.y, diffVecSum.x);
        //         
        //         //Radは外部で使う値なので左右反転の設定に基づいてひっくり返し、Sin/Cosは内部計算用なのでそのままにする
        //         CurrentFaceRollSin = Mathf.Sin(faceRollRad);
        //         CurrentFaceRollCos = Mathf.Cos(faceRollRad);
        //         CurrentFaceRollRad = Parent.DisableHorizontalFlip ? -faceRollRad : faceRollRad;
        //         
        //         //外形の3点だけで顔の矩形計算には足りる(しかもその方が回転不変で良い)
        //         FaceSize = new Vector2(
        //             Vector2.Distance(positions[0], positions[16]),
        //             Vector2.Distance(0.5f * (positions[16] + positions[0]), positions[8])
        //             );
        //     }
        //
        //     public void UpdateYaw(Vector2 mouthCenter)
        //     {
        //         float diffLeft = Vector2.Distance(mouthCenter, Positions[4]);
        //         float diffRight = Vector2.Distance(mouthCenter, Positions[12]);
        //
        //         //ピクセル単位のハズなので1以下ならどちらかの点に被っている(※通常は起きない)
        //         //通常ケースでは(遠いほうの距離 / 近いほうの距離)の比率をうまく畳んで[-1, 1]の範囲に収めようとしている
        //
        //         float yawRate =
        //             (diffLeft < 1f) ? -1f :
        //             (diffRight < 1f) ? 1f :
        //             (diffLeft < diffRight) ? 
        //                 -Mathf.Clamp(diffRight / diffLeft - 1, 0, YawMouthDistanceRatio) / YawMouthDistanceRatio :
        //                 Mathf.Clamp(diffLeft / diffRight - 1, 0, YawMouthDistanceRatio) / YawMouthDistanceRatio;
        //         
        //         CurrentFaceYawRate = Parent.DisableHorizontalFlip ? -yawRate : yawRate;
        //     }
        //
        //     public void UpdatePitch(Vector2 leftEyeCenter, Vector2 rightEyeCenter)
        //     {
        //         //顔のY平均と、目のY平均の上下関係から推定する。
        //         //目のほうが上にある場合は上をむいており、逆もしかり。
        //         var faceY = 0.5f * GetRollCanceled(Positions[0] + Positions[16]).y;
        //         var eyesY = 0.5f * GetRollCanceled(leftEyeCenter + rightEyeCenter).y;
        //         
        //         //POINT: FaceSize.yではなくxによって正規化する。
        //         //Yは口の開閉とか、鎖骨付近を誤検出したときにブレちゃうので、
        //         //そうした外れ値を安全に避けるのが狙い。
        //         EyeFaceYDiff = (eyesY - faceY) / FaceSize.x / EyeFaceYDiffMax;
        //     }
        //
        //     public override void LerpToDefault(CalibrationData calibration, float lerpFactor)
        //     {
        //         CurrentFaceRollRad = Mathf.Lerp(CurrentFaceRollRad, 0, lerpFactor);
        //         CurrentFaceRollSin = Mathf.Sin(CurrentFaceRollSin);
        //         CurrentFaceRollCos = Mathf.Cos(CurrentFaceRollCos);
        //         CurrentFaceYawRate = Mathf.Lerp(CurrentFaceYawRate, 0, lerpFactor);
        //         EyeFaceYDiff = Mathf.Lerp(EyeFaceYDiff, calibration.eyeFaceYDiff, lerpFactor);
        //         
        //         float faceLen = Mathf.Sqrt(calibration.faceSize);
        //         FaceSize = Vector2.Lerp(FaceSize, new Vector2(faceLen, faceLen), lerpFactor);
        //     }
        //     
        //     public override int LandmarkStartIndex => 0;
        //     public override int LandmarkLength => 17;
        // }
        //
        // public class NosePart : FacePartBase
        // {
        //     public NosePart(FaceParts parent) : base(parent)
        //     {
        //     }
        //
        //     public override void LerpToDefault(CalibrationData calibration, float lerpFactor)
        //     {
        //         //何もしないでOK
        //     }
        //
        //     public override int LandmarkStartIndex => 0;
        //     public override int LandmarkLength => 2;
        //
        //     public Vector2 NoseBasePoint => Positions[1];
        // }
        //
        // public abstract class EyePartBase : FacePartBase
        // {
        //     public EyePartBase(FaceParts parent, NosePart nose) : base(parent)
        //     {
        //         _nose = nose;
        //         //デフォルトは開いた状態
        //         CurrentEyeOpenValue = 1.0f;
        //     }
        //
        //     protected readonly NosePart _nose;
        //
        //     //TODO: ここにFaceTrackerToEyeOpenの実装があると筋がいいのかも
        //     public float GetEyeOpenValue()
        //     {
        //         var positions = Positions;
        //         float yMax = positions[0].y;
        //         float yMin = positions[0].y;
        //         for (int i = 1; i < positions.Length; i++)
        //         {
        //             if (yMax < positions[i].y)
        //             {
        //                 yMax = positions[i].y;
        //             }
        //             if (yMin > positions[i].y)
        //             {
        //                 yMin = positions[i].y;
        //             }
        //         }
        //
        //         float rawValue = yMax - yMin;
        //         float normalizedValue = rawValue / Parent.FaceSize.y;
        //         CurrentEyeOpenValue = normalizedValue;
        //
        //         return normalizedValue;
        //     }
        //
        //     public Vector2 GetCenter() => GetCenterPosition();
        //     
        //     protected override void OnUpdated()
        //     {
        //         base.OnUpdated();
        //         CurrentEyeOpenValue = GetEyeOpenValue();
        //     }
        //
        //     public override void LerpToDefault(CalibrationData calibration, float lerpFactor)
        //     {
        //         CurrentEyeOpenValue = Mathf.Lerp(CurrentEyeOpenValue, calibration.eyeOpenHeight, lerpFactor);
        //     }
        //
        //     public float CurrentEyeOpenValue { get; private set; }
        // }
        //
        // public class RightEyePart : EyePartBase
        // {
        //     public RightEyePart(FaceParts parent, NosePart nose) : base(parent, nose) { }
        //     public override int LandmarkStartIndex => 36;
        //     public override int LandmarkLength => 6;
        // }
        //
        // public class LeftEyePart : EyePartBase
        // {
        //     public LeftEyePart(FaceParts parent, NosePart nose) : base(parent, nose) { }
        //     public override int LandmarkStartIndex => 42;
        //     public override int LandmarkLength => 6;
        // }
        //
        // public class MouthPart : FacePartBase
        // {
        //     public MouthPart(FaceParts parent) : base(parent) { }
        //
        //     public override void LerpToDefault(CalibrationData calibration, float lerpFactor)
        //     {
        //         //何もしない: そもそもプロパティとして外に出してない
        //     }
        //
        //     public Vector2 CurrentCenterPosition { get; private set; }
        //
        //     public override int LandmarkStartIndex => 13;
        //     public override int LandmarkLength => 4;
        //     
        //     protected override void OnUpdated()
        //     {
        //         base.OnUpdated();
        //         CurrentCenterPosition = GetCenterPosition();
        //     }
        //     
        // }
    }

}
