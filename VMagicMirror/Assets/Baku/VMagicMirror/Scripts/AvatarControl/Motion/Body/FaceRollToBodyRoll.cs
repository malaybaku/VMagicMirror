using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 顔のロール回転を体の自然な(無意識運動としての)ロール回転に変換するすごいやつだよ
    /// </summary>
    public sealed class FaceRollToBodyRoll
    {
        //最終的に胴体ロールは頭ロールの何倍であるべきか、という値
        private float GoalRate => EnableTwistMotion ? -0.05f : 0.1f;

        //ゴールに持ってくときの速度基準にする時定数っぽいやつ
        private const float TimeFactor = 0.3f;
        
        //ゴール回転値に持ってくとき、スピードに掛けるダンピング項
        private const float SpeedDumpFactor = 0.95f;

        //ゴール回転値に持っていくとき、スピードをどのくらい素早く適用するか
        private float SpeedLerpFactor => EnableTwistMotion ? 24f : 18f;
        
        public bool EnableTwistMotion { get; set; }
        
        private float _speedDegreePerSec = 0;
        public float RollAngleDegree { get; private set; }

        //NOTE: この値はフィルタされてない生のやつ
        public float RawTargetAngle { get; private set; }

        public float FactoredRawTargetAngle => RawTargetAngle * GoalRate;

        public void UpdateSuggestAngle()
        {
            float idealSpeed = (RawTargetAngle * GoalRate - RollAngleDegree) / TimeFactor;
            _speedDegreePerSec = Mathf.Lerp(
                _speedDegreePerSec,
                idealSpeed,
                SpeedLerpFactor * Time.deltaTime
            );

            _speedDegreePerSec *= SpeedDumpFactor;
            RollAngleDegree += _speedDegreePerSec * Time.deltaTime;
        }

        public void SetZeroTarget()
        {
            RawTargetAngle = 0;
        }
        
        public void CheckAngle(Quaternion headRotation)
        {
            //ロールって言ってるけど人間の首は決してUnityのYZXの順で回転するわけじゃないので、実際の計算はファジーにやります。
            //→耳が下とか上を向くのを以て首かしげ運動と見る。
            var rotatedRight = headRotation * Vector3.right;
            RawTargetAngle = Mathf.Asin(rotatedRight.y) * Mathf.Rad2Deg;
        }
    }
}
