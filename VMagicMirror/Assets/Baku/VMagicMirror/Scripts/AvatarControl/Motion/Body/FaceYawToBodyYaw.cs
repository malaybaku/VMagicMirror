using UnityEngine;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 画像ベースの顔のヨー回転を体の自然な(無意識運動としての)ヨー回転に変換するすごいやつだよ
    /// </summary>
    public sealed class FaceYawToBodyYaw
    {
        //最終的に胴体ヨーは頭ヨーの何倍であるべきか、という値
        private const float GoalRate = 0.3f;
        //ゴールに持ってくときの速度基準にする時定数っぽいやつ
        private const float TimeFactor = 0.15f;
        //ゴール回転値に持ってくとき、スピードに掛けるダンピング項
        private const float SpeedDumpFactor = 0.98f;
        //ゴール回転値に持っていくとき、スピードをどのくらい素早く適用するか
        private const float SpeedLerpFactor = 12.0f;
        
        private float _speedDegreePerSec = 0;
        public float YawAngleDegree { get; private set; }

        //NOTE: この値はフィルタされてない生のやつ
        private float _targetAngleDegree = 0;
        
        public void UpdateSuggestAngle()
        {            
            float idealSpeed = (_targetAngleDegree * GoalRate - YawAngleDegree) / TimeFactor;
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
            _targetAngleDegree = 0;
        }

        public void CheckAngle(Quaternion headRotation)
        {
            //首の回転ベースで正面向きがどうなったか見る: コレでうまく動きます
            var headForward = headRotation * Vector3.forward;
            _targetAngleDegree = -(Mathf.Atan2(headForward.z, headForward.x) * Mathf.Rad2Deg - 90);
        }
    }
}
