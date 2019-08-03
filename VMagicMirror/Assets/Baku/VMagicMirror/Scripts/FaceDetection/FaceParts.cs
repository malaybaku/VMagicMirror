using System;
using System.Collections.Generic;
//using System.Linq;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror
{
    //NOTE: いったん簡単な実装で載せたいので、番号付けをいい加減にして統計情報チックな扱いに留める。

    /// <summary>
    /// 68点ランドマークの顔情報をもとに顔の個別パーツの情報を更新するクラス
    /// </summary>
    public class FaceParts
    {
        public const int FaceLandmarkCount = 68;

        private readonly Vector2[] _landmarks = new Vector2[FaceLandmarkCount];

        public FaceParts()
        {
            Outline = new FaceOutlinePart(this);

            RightEyebrow = new RightEyebrowPart(this);
            LeftEyebrow = new LeftEyebrowPart(this);

            Nose = new NosePart(this);

            RightEye = new RightEyePart(this);
            LeftEye = new LeftEyePart(this);

            InnerMouth = new InnerMouthPart(this);
            OuterMouth = new OuterMouthPart(this);

            PartsWithoutOutline = new FacePartBase[]
            {
                RightEyebrow,
                LeftEyebrow,
                Nose,
                RightEye,
                LeftEye,
                InnerMouth,
                OuterMouth,
            };
        }

        public Vector2 FaceSize { get; private set; } = Vector2.one;

        public FaceOutlinePart Outline { get; }

        public RightEyebrowPart RightEyebrow { get; }
        public LeftEyebrowPart LeftEyebrow { get; }

        public NosePart Nose { get; }

        public RightEyePart RightEye { get; }
        public LeftEyePart LeftEye { get; }

        public InnerMouthPart InnerMouth { get; }
        public OuterMouthPart OuterMouth { get; }

        //輪郭は顔サイズと傾き除去のために特別扱いしたいので外す
        private FacePartBase[] PartsWithoutOutline { get; }

        //todo: 通常、目閉じ、目の見開き、で3つのキャリブがあった方がよさそうな…
        public void Calibrate(IList<Vector2> landmarks)
        {
            if (landmarks == null || landmarks.Count < FaceLandmarkCount)
            {
                return;
            }

            var landmarksArray = new Vector2[FaceLandmarkCount];
            for(int i = 0; i < landmarksArray.Length; i++)
            {
                landmarksArray[i] = landmarks[i];
            }

            Outline.Calibrate(landmarksArray);
            foreach (var facePart in PartsWithoutOutline)
            {
                facePart.Calibrate(landmarksArray);
            }
        }

        public void Update(Rect mainPersonRect, List<Vector2> landmarks)
        {
            if (landmarks == null || landmarks.Count < FaceLandmarkCount)
            {
                return;
            }

            //顔の中心でオフセット取る : たぶん回転の除去とかで都合がよいのでこのスタイルで行きます
            var offset = mainPersonRect.center;
            for(int i = 0; i < _landmarks.Length; i++)
            {
                _landmarks[i] = landmarks[i] - offset;
            }

            Outline.Update(_landmarks);
            FaceSize = Outline.FaceSize;
            for (int i = 0; i < PartsWithoutOutline.Length; i++)
            {
                PartsWithoutOutline[i].Update(_landmarks);
            }
            Outline.UpdateYaw(InnerMouth.CurrentCenterPosition);
        }
 
        //todo: 内部実装をSpanでやった方がカッコ良さそう…
        public abstract class FacePartBase
        {
            protected FacePartBase(FaceParts parent)
            {
                _positions = new Vector2[LandmarkLength];
                Parent = parent;
            }
            private readonly Vector2[] _positions;

            public FaceParts Parent { get; }

            /// <summary>
            /// WARN: GCAllocをラクに避けるためにこう書いてるが派生クラスでの書き込みはダメ！
            /// </summary>
            public Vector2[] Positions => _positions;

            public void Calibrate(Vector2[] rects)
            {
                //NOTE: いったん共通処理は無し。
                OnCalibrated();
            }

            public void Update(Vector2[] rects)
            {
                //ややチェックが雑だが本ファイルの範囲でしか使わないから大丈夫なはず
                Array.Copy(rects, LandmarkStartIndex, _positions, 0, LandmarkLength);
                OnUpdated();
            }

            protected void CancelRollRotation()
            {
                //sinにマイナスがつくのは、キャンセル回転のためにもとの角度を(-1)倍した値のsin,cosをセットで得るため
                float cos = Parent.Outline.CurrentFaceRollCos;
                float sin = -Parent.Outline.CurrentFaceRollSin;
                for(int i = 0; i < _positions.Length; i++)
                {
                    var p = _positions[i];
                    _positions[i] = new Vector2(
                        p.x * cos - p.y * sin,
                        p.x * sin + p.y * cos
                        );
                }
            }

            /// <summary>この顔パーツが属している特徴点の図心を計算する。</summary>
            /// <returns></returns>
            protected Vector2 GetCenterPosition()
            {
                //NOTE: LINQ使ってないのはパフォーマンス配慮
                var sum = new Vector2();
                var pos = Positions;
                for (int i = 0; i < pos.Length; i++)
                {
                    sum += pos[i];
                }
                return sum / pos.Length;
            }

            protected virtual void OnCalibrated() { }
            protected virtual void OnUpdated() { }

            public virtual bool HasValidData => _positions.Length > 0;

            public abstract int LandmarkStartIndex { get; }
            public abstract int LandmarkLength { get; }
        }

        public class FaceOutlinePart : FacePartBase
        {
            //顔のヨー角を輪郭と口の中心のキョリの非で求める際に用いる比率。
            //値が大きいほど、ヨー角が小さくなる。
            private const float YawMouthDistanceRatio = 3.0f;

            public FaceOutlinePart(FaceParts parent) : base(parent)
            {

            }

            public Vector2 FaceSize { get; private set; } = Vector2.one;

            private readonly Subject<Vector2> _faceSize = new Subject<Vector2>();
            public IObservable<Vector2> FaceSizeObservable => _faceSize;

            private readonly Subject<float> _headRollRad = new Subject<float>();
            public IObservable<float> HeadRollRad => _headRollRad;

            private readonly Subject<float> _headYawRate = new Subject<float>();
            public IObservable<float> HeadYawRate => _headYawRate;

            protected override void OnUpdated()
            {
                var positions = Positions;

                //輪郭の端、つまり両こめかみ付近に線を引いてみたときの傾きをとっている。
                //以前はアゴ先の位置も考慮していたが、それだとヨー運動と合成されてしまうため、使わないようにした。
                Vector2 diffVecSum = positions[16] - positions[0];
                CurrentFaceRollRad = Mathf.Atan2(diffVecSum.y, diffVecSum.x);
                CurrentFaceRollSin = Mathf.Sin(CurrentFaceRollRad);
                CurrentFaceRollCos = Mathf.Cos(CurrentFaceRollRad);

                _headRollRad.OnNext(CurrentFaceRollRad);

                //外形の3点だけで顔の矩形計算には足りる(しかもその方が回転不変で良い)
                FaceSize = new Vector2(
                    Vector2.Distance(positions[0], positions[16]),
                    Vector2.Distance(0.5f * (positions[16] + positions[0]), positions[8])
                    );

                _faceSize.OnNext(FaceSize);
            }

            public void UpdateYaw(Vector2 mouthCenter)
            {
                float diffLeft = Vector2.Distance(mouthCenter, Positions[4]);
                float diffRight = Vector2.Distance(mouthCenter, Positions[12]);

                //ピクセル単位のハズなので1以下ならどちらかの点に被っている(※通常は起きない)
                //通常ケースでは(遠いほうの距離 / 近いほうの距離)の比率をうまく畳んで[-1, 1]の範囲に収めようとしている
                CurrentFaceYawRate =
                    (diffLeft < 1f) ? -1f :
                    (diffRight < 1f) ? 1f :
                    (diffLeft < diffRight) ? 
                        -Mathf.Clamp(diffRight / diffLeft - 1, 0, YawMouthDistanceRatio) / YawMouthDistanceRatio :
                        Mathf.Clamp(diffLeft / diffRight - 1, 0, YawMouthDistanceRatio) / YawMouthDistanceRatio;
                _headYawRate.OnNext(CurrentFaceYawRate);
            }

            public override int LandmarkStartIndex => 0;
            public override int LandmarkLength => 17;

            public float CurrentFaceRollRad { get; private set; }
            //NOTE: sin, cosは回転計算で何度も欲しいのでキャッシュしとく
            public float CurrentFaceRollSin { get; private set; }
            public float CurrentFaceRollCos { get; private set; }

            /// <summary>
            /// 左右どちらを向いているかを[-1(左), 1(右)]の範囲で表すレート。
            /// </summary>
            /// <remarks>
            /// 計算上レートと角度は比例しないが、近似として比例扱いにしても良い。
            /// </remarks>
            public float CurrentFaceYawRate { get; private set; }
        }

        public abstract class EyebrowPartBase : FacePartBase
        {
            public EyebrowPartBase(FaceParts parent) : base(parent)
            {
            }

            protected override void OnUpdated()
            {
                CancelRollRotation();

                var positions = Positions;
                float ySum = 0;
                for (int i = 0; i < positions.Length; i++)
                {
                    ySum += positions[i].y;
                }

                float height = ySum / positions.Length;
                float normalizedHeight = (Parent.FaceSize.y - height) / Parent.FaceSize.y;
                CurrentHeight = normalizedHeight;
                _height.OnNext(normalizedHeight);
            }

            public float CurrentHeight { get; private set; }

            protected Subject<float> _height = new Subject<float>();
            public IObservable<float> Height => _height;
        }

        public class RightEyebrowPart : EyebrowPartBase
        {
            public RightEyebrowPart(FaceParts parent) : base(parent)
            {
            }
            public override int LandmarkStartIndex => 17;
            public override int LandmarkLength => 5;
        }

        public class LeftEyebrowPart : EyebrowPartBase
        {
            public LeftEyebrowPart(FaceParts parent) : base(parent)
            {
            }
            public override int LandmarkStartIndex => 22;
            public override int LandmarkLength => 5;
        }

        public class NosePart : FacePartBase
        {
            public NosePart(FaceParts parent) : base(parent)
            {
            }
            public override int LandmarkStartIndex => 27;
            public override int LandmarkLength => 9;

            /// <summary>Index 30に鼻先のとがった所の位置が入る</summary>
            public const int NoseBaseTopIndex = 3;
            /// <summary>Index 33に下側の鼻の付け根の位置が入る</summary>
            public const int NoseBaseBottomIndex = 6;

            public float GetNoseBaseHeightValue()
            {
                var positions = Positions;
                //画像座標だと下に行くほどプラスなので、こうやると値がプラスになって都合がよい
                float rawValue = positions[NoseBaseBottomIndex].y - positions[NoseBaseTopIndex].y;
                float normalizedValue = rawValue / Parent.FaceSize.y;
                return normalizedValue;
            }

            protected override void OnUpdated()
            {
                CurrentNoseBaseHeightValue = GetNoseBaseHeightValue();
                _noseBaseHeightValue.OnNext(CurrentNoseBaseHeightValue);
            }

            private readonly Subject<float> _noseBaseHeightValue = new Subject<float>();
            public float CurrentNoseBaseHeightValue { get; private set; }

            /// <summary>
            /// 鼻底が作るタコ型四角形の、タテ方向の長さを顔の長さで割って正規化した(0.1程度の)値。
            /// 顔の前後の傾斜の指標として利用可能。
            /// </summary>
            /// <returns></returns>
            public IObservable<float> NoseBaseHeightValue => _noseBaseHeightValue;
        }

        public abstract class EyePartBase : FacePartBase
        {
            public EyePartBase(FaceParts parent) : base(parent)
            {
                //デフォルトは開いた状態
                CurrentEyeOpenValue = 1.0f;
            }

            public float GetEyeOpenValue()
            {
                var positions = Positions;
                float yMax = positions[0].y;
                float yMin = positions[0].y;
                for (int i = 1; i < positions.Length; i++)
                {
                    if (yMax < positions[i].y)
                    {
                        yMax = positions[i].y;
                    }
                    if (yMin > positions[i].y)
                    {
                        yMin = positions[i].y;
                    }
                }

                float rawValue = yMax - yMin;
                //float rawValue = positions.Max(r => r.y) - positions.Min(r => r.y);
                float normalizedValue = rawValue / Parent.FaceSize.y;
                CurrentEyeOpenValue = normalizedValue;

                return normalizedValue;
            }

            protected override void OnUpdated()
            {
                base.OnUpdated();
                _eyeOpenValue.OnNext(GetEyeOpenValue());
            }

            private readonly Subject<float> _eyeOpenValue = new Subject<float>();
            public float CurrentEyeOpenValue { get; private set; }

            public IObservable<float> EyeOpenValue => _eyeOpenValue;

            public const float EyeOpenSizeMin = 0.02f;
            public const float EyeOpenSizeMax = 0.06f;
        }

        public class RightEyePart : EyePartBase
        {
            public RightEyePart(FaceParts parent) : base(parent) { }
            public override int LandmarkStartIndex => 36;
            public override int LandmarkLength => 6;
        }

        public class LeftEyePart : EyePartBase
        {
            public LeftEyePart(FaceParts parent) : base(parent) { }
            public override int LandmarkStartIndex => 42;
            public override int LandmarkLength => 6;
        }

        public class OuterMouthPart : FacePartBase
        {
            public OuterMouthPart(FaceParts parent) : base(parent) { }

            public override int LandmarkStartIndex => 60;
            public override int LandmarkLength => 8;
        }

        public class InnerMouthPart : FacePartBase
        {
            public InnerMouthPart(FaceParts parent) : base(parent) { }

            protected override void OnUpdated()
            {
                base.OnUpdated();
                CurrentCenterPosition = GetCenterPosition();
            }

            public Vector2 CurrentCenterPosition { get; private set; }

            public override int LandmarkStartIndex => 48;
            public override int LandmarkLength => 12;
        }

    }

}
