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
        public float SpeedFactor { get; set; } = 6.0f;
        public bool UseZAngle { get; set; } = false;

        public event Action<Vector3> JitterTargetEulerUpdated;
        
        public Quaternion CurrentRotation { get; private set; } = Quaternion.identity;
        public Quaternion TargetRotation { get; private set; } = Quaternion.identity;
        
        private float _count = 0.0f;

        public void Reset()
        {
            _count = 0;
            TargetRotation = Quaternion.identity;
            CurrentRotation = Quaternion.identity;
        }
        
        public void Update(float dt)
        {
            _count -= dt;
            if (_count < 0)
            {
                _count = Random.Range(ChangeTimeMin, ChangeTimeMax);
                var targetEuler = new Vector3(
                    Random.Range(-AngleRange.x, +AngleRange.x),
                    Random.Range(-AngleRange.y, +AngleRange.y), 
                    UseZAngle ? Random.Range(-AngleRange.z, +AngleRange.z) : 0f
                    );
                TargetRotation = Quaternion.Euler(targetEuler);
                JitterTargetEulerUpdated?.Invoke(targetEuler);
            }

            CurrentRotation = Quaternion.Slerp(
                CurrentRotation,
                TargetRotation,
                SpeedFactor * dt
                );
        }
    }
}
