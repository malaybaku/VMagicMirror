﻿using UnityEngine;

namespace Baku.VMagicMirror
{
    //ref: https://gist.github.com/GOROman/51ee32887bd1d3248b7610f845904b30
    /// <summary> 眼球微細運動をするやつ </summary>
    public class EyeJitter : MonoBehaviour, IEyeRotationRequestSource
    {
        [Tooltip("値が変化する最小の時間間隔")]
        [SerializeField] private float changeTimeMin = 0.4f;
        
        [Tooltip("値が変化する最大の時間間隔")]
        [SerializeField] private float changeTimeMax = 2.0f;
        
        [Tooltip("可動範囲(比率ベース")]
        [SerializeField] private Vector2 rateRange = new Vector2(0.5f, 0.3f);

        [Tooltip("微細運動をスムージングする速度ファクタ")]
        [SerializeField] private float speedFactor = 11.0f;

        private float _count = 0.0f;
        private Vector2 _targetRate = Vector2.zero;
        private Vector2 _rate = Vector3.zero;

        public bool IsActive { get; set; }
        public Vector2 LeftEyeRotationRate => _rate;
        public Vector2 RightEyeRotationRate => _rate;

        private void Update()
        {
            _count -= Time.deltaTime;
            if (_count < 0)
            {
                _count = Random.Range(changeTimeMin, changeTimeMax);
                _targetRate = new Vector2(
                    Random.Range(-rateRange.x, rateRange.x),
                    Random.Range(-rateRange.y, rateRange.y)
                );
            }
            _rate = Vector2.Lerp(_rate, _targetRate, speedFactor * Time.deltaTime);
        }
    }
}
