using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 画像ベースの顔のヨー回転を体の自然な(無意識運動としての)ヨー回転に変換するすごいやつだよ
    /// </summary>
    public sealed class FaceYawToBodyYaw
    {
        //ゴールに持ってくときの速度基準にする時定数っぽいやつ
        private const float TimeFactor = 0.15f;
        //ゴール回転値に持ってくとき、スピードに掛けるダンピング項
        private const float SpeedDumpFactor = 0.98f;

        //最終的に胴体ヨーは頭ヨーの何倍であるべきか、という値
        // 100%を超えた分は肩より動きを大きくし、かつ素早く動くようにする
        private float GoalRate => EnableTwistMotion
            ? -0.2f * (TwistMotionStrength + Mathf.Max(0f, TwistMotionStrength - 1f))
            : 0.3f;
        //ゴール回転値に持っていくとき、スピードをどのくらい素早く適用するか
        private float SpeedLerpFactor => EnableTwistMotion
            ? 18f * Mathf.Lerp(1f, 3f, TwistMotionStrength - 1f)
            : 12f;
        
        public bool EnableTwistMotion { get; set; }
        public float TwistMotionStrength { get; set; } = 1f;
        
        private float _speedDegreePerSec = 0;
        public float YawAngleDegree { get; private set; }

        //NOTE: この値はフィルタされてない生のやつ
        public float RawTargetAngle { get; private set; }
        
        public void UpdateSuggestAngle()
        {            
            float idealSpeed = (RawTargetAngle * GoalRate - YawAngleDegree) / TimeFactor;
            _speedDegreePerSec = Mathf.Lerp(
                _speedDegreePerSec,
                idealSpeed,
                SpeedLerpFactor * Time.deltaTime
            );

            _speedDegreePerSec *= SpeedDumpFactor;
            YawAngleDegree += _speedDegreePerSec * Time.deltaTime;
        }

        public void SetZeroTarget()
        {
            RawTargetAngle = 0;
        }

        public void CheckAngle(Quaternion headRotation)
        {
            //首の回転ベースで正面向きがどうなったか見る: コレでうまく動きます
            var headForward = headRotation * Vector3.forward;
            RawTargetAngle = -(Mathf.Atan2(headForward.z, headForward.x) * Mathf.Rad2Deg - 90);
        }
    }
}
