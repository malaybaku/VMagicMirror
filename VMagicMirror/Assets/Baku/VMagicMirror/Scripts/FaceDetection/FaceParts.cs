using System;
using System.Collections.Generic;
using System.Linq;
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

        //輪郭は顔サイズの計算のために特別扱いしたいから外す
        private FacePartBase[] PartsWithoutOutline => new FacePartBase[]
        {
            RightEyebrow,
            LeftEyebrow,
            Nose,
            RightEye,
            LeftEye,
            InnerMouth,
            OuterMouth,
        };

        //todo: 通常、目閉じ、目の見開き、で3つのキャリブがあった方がよさそうな…
        public void Calibrate(IList<Vector2> landmarks)
        {
            if (landmarks == null || landmarks.Count < FaceLandmarkCount)
            {
                return;
            }

            var landmarksArray = landmarks.ToArray();

            Outline.Calibrate(landmarksArray);
            foreach (var facePart in PartsWithoutOutline)
            {
                facePart.Calibrate(landmarksArray);
            }
        }

        public void Update(IList<Vector2> landmarks)
        {
            if (landmarks == null || landmarks.Count < FaceLandmarkCount)
            {
                return;
            }

            var landmarksArray = landmarks.ToArray();
            Outline.Update(landmarksArray);
            FaceSize = Outline.FaceSize;
            foreach (var facePart in PartsWithoutOutline)
            {
                facePart.Update(landmarksArray);
            }
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

            public Vector2[] Positions
            {
                get
                {
                    var result = new Vector2[_positions.Length];
                    Array.Copy(_positions, result, _positions.Length);
                    return result;
                }
            }

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
        
            protected virtual void OnCalibrated() { }
            protected virtual void OnUpdated() { }

            public virtual bool HasValidData => _positions.Length > 0;

            public abstract int LandmarkStartIndex { get; }
            public abstract int LandmarkLength { get; }
        }

        public class FaceOutlinePart : FacePartBase
        {
            public FaceOutlinePart(FaceParts parent) : base(parent)
            {

            }

            public Vector2 FaceSize { get; private set; } = Vector2.one;

            private readonly Subject<float> _faceOrientationOffset = new Subject<float>();
            public IObservable<float> FaceOrientationOffset => _faceOrientationOffset;

            protected override void OnUpdated()
            {
                base.OnUpdated();

                var positions = Positions;
                FaceSize = new Vector2(
                    positions.Max(p => p.x) - positions.Min(p => p.x),
                    positions.Max(p => p.y) - positions.Min(p => p.y)
                    );

                //アゴがどっちに向いているのかを、輪郭の端と真ん中でざっくりした方向ベクトルを作って判別
                Vector2 diffVecSum = 2 * positions[8] - positions[0] - positions[16];
                _faceOrientationOffset.OnNext(
                    Mathf.Atan2(-diffVecSum.x, diffVecSum.y)
                    );
            }

            public override int LandmarkStartIndex => 0;
            public override int LandmarkLength => 17;
        }

        public abstract class EyebrowPartBase : FacePartBase
        {
            public EyebrowPartBase(FaceParts parent) : base(parent)
            {
            }
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
                float rawValue = positions.Max(r => r.y) - positions.Min(r => r.y);
                float normalizedValue = rawValue / Parent.FaceSize.y;

                return Mathf.Clamp(
                    (normalizedValue - EyeOpenSizeMin) / (EyeOpenSizeMax - EyeOpenSizeMin), 0, 1
                    );
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

            public override int LandmarkStartIndex => 48;
            public override int LandmarkLength => 12;
        }

    }

}
