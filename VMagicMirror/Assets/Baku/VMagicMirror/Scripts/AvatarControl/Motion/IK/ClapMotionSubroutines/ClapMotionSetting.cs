using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 首の動きだけからなるようなモーションの再生の設定
    /// ほぼハードコーディング
    /// </summary>
    [CreateAssetMenu(menuName = "VMagicMirror/ClapMotionSetting")]
    public class ClapMotionSetting : ScriptableObject
    {
        [Serializable]
        public class ClapMotionItem
        {
            //NOTE:
            // - 全てのcurveの横軸は時間であり、[0, 1]に正規化する。
            // - x offset curveは縦軸も[0, 1]正規化する
            // - z rot curveも同じく、縦軸は[0, 1]正規化する
            // - finger bend curve系のcurveの値は正規化せず、曲げ角度をプラスの値で指定する
            //   - 親指の事は諦めて…
            // - 

            //entry系のカーブは1回目の拍手動作に使い、これは2回目以降のよりも動きが大きい
            [SerializeField] public AnimationCurve entryXOffsetCurve;
            [SerializeField] public AnimationCurve entryZRotationCurve;
            [SerializeField] public AnimationCurve entryFingerBendAngleCurve;

            [SerializeField] public AnimationCurve xOffsetCurve;
            [SerializeField] public AnimationCurve zRotationCurve;
            [SerializeField] public AnimationCurve fingerBendAngleCurve;
            
            [SerializeField] public float durationPerClapMin = 0.25f;
            [SerializeField] public float durationPerClapMax = 0.3f;

            [SerializeField] public Vector3 centerRotOffsetEulerMin = new(-5f, -10f, -6f);
            [SerializeField] public Vector3 centerRotOffsetEulerMax = new(5f, 10f, 6f);

            [SerializeField] public float offsetScaleMin = 0.85f;
            [SerializeField] public float offsetScaleMax = 1f;

            //左右の手でピッチ角度がちょっとズレているべきなので、そのずれ量をdegreeベースで指定する
            [SerializeField] public float pitchDiffMin = 5f;
            [SerializeField] public float pitchDiffMax = 15f;

            [SerializeField] public int clapCountMin = 6;
            [SerializeField] public int clapCountMax = 17;
        }

        [SerializeField] private ClapMotionItem item;

        public ClapMotionParams CreateMotionParams(float entryDuration, float xOffset, float xMoveLength, float zOffsetDeg) =>
            new(item, entryDuration, xOffset, xMoveLength, zOffsetDeg);


        public readonly struct ClapMotionParams
        {
            public ClapMotionParams(ClapMotionItem item, float entryDuration, float xOffset, float xMoveLength, float zOffsetDeg)
            {
                _item = item;
                _xOffset = xOffset;
                _entryDuration = entryDuration;

                //Randomを呼ぶ以上はファクトリメソッド使った方がしっくり来るんだけど、まあ細かく気にしない方向で…
                ClapCount = Random.Range(item.clapCountMin, item.clapCountMax + 1);
            
                //NOTE: ナイーブに考えると「小さい動き==素早い拍手」なんだけど、振れ幅を抑えているので一旦気にしすぎない方向で。
                var motionSizeFactor = Random.Range(item.offsetScaleMin, item.offsetScaleMax);
                _xMoveLength = xMoveLength * motionSizeFactor;
                _zOffsetDeg = zOffsetDeg * motionSizeFactor;
                DurationPerClap = Random.Range(item.durationPerClapMin, item.durationPerClapMax);
                MainClapDuration = DurationPerClap * (ClapCount - 1);
                Duration = _entryDuration + MainClapDuration;

                //NOTE: この値は完全にテキトーな乱数で、呼び出し元で拍手時の位置をずらしたければ勝手にすれば、という程度の意味
                PositionOffsetRate = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                );

                _pitchDiff = Random.Range(item.pitchDiffMin, item.pitchDiffMax);
                _centerRotation = Quaternion.Euler(
                    Random.Range(item.centerRotOffsetEulerMin.x, item.centerRotOffsetEulerMax.x),
                    Random.Range(item.centerRotOffsetEulerMin.y, item.centerRotOffsetEulerMax.y),
                    Random.Range(item.centerRotOffsetEulerMin.z, item.centerRotOffsetEulerMax.z)
                );
            }

            private readonly ClapMotionItem _item;
            private readonly float _entryDuration;
            private readonly float _xOffset;
            private readonly float _xMoveLength;
            private readonly float _zOffsetDeg;
            private readonly float _pitchDiff;

            public readonly int ClapCount;

            public readonly float DurationPerClap;
            //NOTE: 初回パチパチ部分だけのDurationが入り、手を構えたり戻したりする部分は考慮しないものとする。
            public readonly float MainClapDuration;
            public readonly float Duration;
            
            public Vector3 PositionOffsetRate { get; }
            private readonly Quaternion _centerRotation;

            //NOTE: 1回目の拍手
            public ClapMotionFrameData Evaluate(float time)
            {
                //入り動作:
                if (time < _entryDuration)
                {
                    return EvaluateEntryMotion(time);
                }
                
                //入り動作が終わったあとの規則的な拍手
                var timeRate = Mathf.Repeat(time - _entryDuration, DurationPerClap) / DurationPerClap;
                var xOffset = _xOffset + _xMoveLength * _item.xOffsetCurve.Evaluate(timeRate);
                var elbowOffset = _xOffset + _xMoveLength * _item.xOffsetCurve.Evaluate(timeRate);
                var zRotation = _zOffsetDeg * _item.zRotationCurve.Evaluate(timeRate);
                var fingerBendAngle = _item.fingerBendAngleCurve.Evaluate(timeRate);
                return new ClapMotionFrameData(
                    _centerRotation,
                    new Vector3(xOffset, 0f, 0f),
                    elbowOffset,
                    _pitchDiff,
                    zRotation,
                    fingerBendAngle
                );
            }

            private ClapMotionFrameData EvaluateEntryMotion(float time)
            {
                var timeRate = Mathf.Clamp01(time / DurationPerClap);
                var xOffset = _xOffset + _xMoveLength * _item.entryXOffsetCurve.Evaluate(timeRate);
                var zRotation = _zOffsetDeg * _item.entryZRotationCurve.Evaluate(timeRate);
                var fingerBendAngle = _item.entryFingerBendAngleCurve.Evaluate(timeRate);
                var elbowOffset = _xOffset + _xMoveLength * _item.entryXOffsetCurve.Evaluate(timeRate);
                return new ClapMotionFrameData(
                    _centerRotation,
                    new Vector3(xOffset, 0f, 0f),
                    elbowOffset,
                    _pitchDiff,
                    zRotation,
                    fingerBendAngle
                );
            }
        }

        public readonly struct ClapMotionFrameData
        {
            public ClapMotionFrameData(
                Quaternion centerRotation,
                Vector3 rightPositionOffset,
                float rightElbowOffset,
                float pitchDiff, float rightZRotOffset, float fingerBendAngle)
            {
                RightPositionOffset = centerRotation * rightPositionOffset;
                LeftPositionOffset = centerRotation * new Vector3(
                    -rightPositionOffset.x,
                    rightPositionOffset.y,
                    rightPositionOffset.z
                );
                RightElbowOffset = new Vector3(rightElbowOffset, 0, 0);
                LeftElbowOffset = new Vector3(-rightElbowOffset, 0, 0);

                RightHandRot = centerRotation * Quaternion.Euler(-pitchDiff, 0f, 0f) * Quaternion.Euler(0f, 180f, 90f + rightZRotOffset);
                LeftHandRot = centerRotation * Quaternion.Euler(pitchDiff, 0f, 0f) * Quaternion.Euler(0f, 180f, -90f - rightZRotOffset);

                FingerBendAngle = fingerBendAngle;
            }
            
            //NOTE: これらのオフセットはワールド座標ベース
            public Vector3 RightPositionOffset { get; }
            public Vector3 LeftPositionOffset { get; }
            
            public Quaternion RightHandRot { get; }
            public Quaternion LeftHandRot { get; }
            
            //NOTE: ひじのBendGoalが連動してたら嬉しいな、という主旨で、RightPositionOffsetと大体同じ意味の値を公開する。
            //本質的には同じじゃなくて良いはずなので別の値にしている。
            public Vector3 RightElbowOffset { get; }
            public Vector3 LeftElbowOffset { get; }
            
            public float FingerBendAngle { get; }
        }
    }
}

