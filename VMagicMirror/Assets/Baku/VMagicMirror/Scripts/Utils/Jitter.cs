using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// EyeJitterみたいな「ランダム周期で原点回転付近をちらちら動く」系の処理をカスタムつきでやってくれるすごいやつだよ
    /// </summary>
    public class Jitter
    {
        public float ChangeTimeMin { get; set; } = 1.5f;
        public float ChangeTimeMax { get; set; } = 6.0f;
        public Vector3 AngleRange { get; set; } = new Vector3(8f, 3f, 3f);

        public float GoalMoveSpeedFactor { get; set; } = 12.0f;
        public float DumpFactor { get; set; } = 10.0f;
        public float PositionFactor { get; set; } = 50.0f;
        public bool UseZAngle { get; set; } = false;

        public event Action<Vector3> JitterTargetEulerUpdated;

        private Vector3 _currentSpeed = Vector3.zero;
        private Vector3 _currentRotationEuler = Vector3.zero;
        private Vector3 _goalRotationEuler = Vector3.zero;
        private Vector3 _targetRotationEuler = Vector3.zero;
        
        public Quaternion CurrentRotation { get; private set; } = Quaternion.identity;
        
        private float _count = 0.0f;

        public void Reset()
        {
            _count = 0;
            CurrentRotation = Quaternion.identity;
            _currentSpeed = Vector3.zero;
            _currentRotationEuler = Vector3.zero;
            _goalRotationEuler = Vector3.zero;
            _targetRotationEuler = Vector3.zero;
        }
        
        public void Update(float dt)
        {
            _count -= dt;
            if (_count < 0)
            {
                _count = Random.Range(ChangeTimeMin, ChangeTimeMax);
                var goalEuler = new Vector3(
                    Random.Range(-AngleRange.x, +AngleRange.x),
                    Random.Range(-AngleRange.y, +AngleRange.y), 
                    UseZAngle ? Random.Range(-AngleRange.z, +AngleRange.z) : 0f
                    );
                _goalRotationEuler = goalEuler;
                JitterTargetEulerUpdated?.Invoke(goalEuler);
            }

            //ゴール自体をLerpする: ゴールが急にジャンプすると力学ライクな計算してもカクカクしちゃうので
            _targetRotationEuler = Vector3.Lerp(
                _targetRotationEuler,
                _goalRotationEuler,
                GoalMoveSpeedFactor * dt
                );
            
            //一次陽的オイラーくんに頑張ってもらいます
            var force = 
                -DumpFactor * _currentSpeed - PositionFactor * (_currentRotationEuler - _targetRotationEuler);
            _currentSpeed += force * dt;
            _currentRotationEuler += _currentSpeed * dt;
            
            //角度が十分小さいからこういうやり方でも成り立つ、はず
            CurrentRotation = Quaternion.Euler(_currentRotationEuler);
        }
    }
}
