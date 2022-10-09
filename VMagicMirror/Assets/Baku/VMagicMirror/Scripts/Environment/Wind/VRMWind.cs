using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniVRM10;
using Zenject;
using Random = UnityEngine.Random;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// VRMの揺れものに対して風を発生させる発生源の処理
    /// </summary>
    public class VRMWind : MonoBehaviour
    {
        [SerializeField] private bool enableWind = true;
        
        //風の基本の方向と、生成したベクトルを適当にずらすファクタ
        //ヨーが0付近のときは正面からの風になる
        //ヨーにプラスの角度を指定すると、画面の右から風が吹いているように扱う
        [SerializeField] private float windYawDegree = 90f;
        [SerializeField] private float windOrientationRandomPower = 0.2f;

        //風の強さ、発生頻度、立ち上がりと立ち下がりの時間を、それぞれ全てRandom.Rangeに通すために幅付きの値にする
        [SerializeField] private Vector2 windStrengthRange = new Vector2(0.03f, 0.06f);
        [SerializeField] private Vector2 windIntervalRange = new Vector2(0.7f, 1.9f);
        [SerializeField] private Vector2 windRiseCountRange = new Vector2(0.4f, 0.6f);
        [SerializeField] private Vector2 windSitCountRange = new Vector2(1.3f, 1.8f);

        //上記の強さと時間に対して定数倍するファクタ
        [SerializeField] private float strengthFactor = 1.0f;
        [SerializeField] private float timeFactor = 1.0f;
        
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, IMessageReceiver receiver)
        {
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmUnloading;
            var _ = new WindSettingReceiver(receiver, this);
        }
        
        class WindItem
        {
            public WindItem(Vector3 orientation, float riseCount, float sitCount, float maxFactor)
            {
                Orientation = orientation;
                RiseCount = riseCount;
                SitCount = sitCount;
                MaxFactor = maxFactor;

                TotalTime = RiseCount + SitCount;
            } 
            
            public Vector3 Orientation { get; }
            public float RiseCount { get; }
            public float SitCount { get; }
            public float MaxFactor { get; }
            public float TotalTime { get; }
            public float TimeCount { get; set; }

            public float CurrentFactor =>
                TimeCount < RiseCount
                ? MaxFactor * TimeCount / RiseCount
                : MaxFactor * (1 - (TimeCount - RiseCount) / SitCount);
        }
        
        private float _windGenerateCount = 0;
        //private VRMSpringBoneJob[] _springBones = new VRMSpringBoneJob[] { };
        private VRM10SpringBoneJoint[] _springBones = Array.Empty<VRM10SpringBoneJoint>();
        private Vector3[] _originalGravityDirections = new Vector3[] { };
        private float[] _originalGravityFactors = new float[] { };
        private readonly List<WindItem> _windItems = new List<WindItem>();

        public bool WindEnabled { get; private set; }

        public float WindYawDegree
        {
            get => windYawDegree;
            set => windYawDegree = value;
        }

        public void EnableWind(bool enable)
        {
            if (WindEnabled != enable)
            {
                WindEnabled = enable;
                enableWind = enable;
                if (!WindEnabled)
                {
                    DisableWind();
                }
            }
        }

        public void SetStrength(float strengthPercentage)
        {
            strengthFactor = strengthPercentage;
        }

        public void SetInterval(float intervalPercentage)
        {
            timeFactor = intervalPercentage;
        }        
        private void DisableWind()
        {
            for (int i = 0; i < _springBones.Length; i++)
            {
                var bone = _springBones[i];
                bone.m_gravityDir = _originalGravityDirections[i];
                bone.m_gravityPower = _originalGravityFactors[i];
            }
        }
        
        private void Update()
        {
            EnableWind(enableWind);
            if (!WindEnabled)
            {
                return;
            }

            UpdateWindGenerateCount();
            UpdateWindItems();
            
            Vector3 localWindForce = Vector3.zero;
            for (int i = 0; i < _windItems.Count; i++)
            {
                localWindForce += _windItems[i].CurrentFactor * _windItems[i].Orientation;
            }

            Vector3 windForce = transform.rotation * localWindForce;
            for (int i = 0; i < _springBones.Length; i++)
            {
                var bone = _springBones[i];
                //NOTE: 力を合成して斜めに力をかけるのが狙い
                var forceSum = _originalGravityFactors[i] * _originalGravityDirections[i] + windForce;
                bone.m_gravityDir = forceSum.normalized;
                bone.m_gravityPower = forceSum.magnitude;
            }
        }

        private void UpdateWindGenerateCount()
        {
            _windGenerateCount -= Time.deltaTime;
            if (_windGenerateCount > 0)
            {
                return;
            }
            _windGenerateCount = Random.Range(windIntervalRange.x, windIntervalRange.y) * timeFactor;
            
            var windBaseOrientation = new Vector3(
                -Mathf.Sin(WindYawDegree * Mathf.Deg2Rad), 0, Mathf.Cos(WindYawDegree * Mathf.Deg2Rad)
                );

            var windOrientation = (
                windBaseOrientation + 
                new Vector3(
                   Random.Range(-windOrientationRandomPower, windOrientationRandomPower),
                   Random.Range(-windOrientationRandomPower, windOrientationRandomPower),
                   Random.Range(-windOrientationRandomPower, windOrientationRandomPower)
                    )).normalized;
            
            _windItems.Add(new WindItem(
                windOrientation,    
                Random.Range(windRiseCountRange.x, windRiseCountRange.y),
                Random.Range(windSitCountRange.x, windSitCountRange.y),
                Random.Range(windStrengthRange.x, windStrengthRange.y) * strengthFactor
            ));
        }

        private void UpdateWindItems()
        {
            //Removeする可能性があるので逆順にやってます
            for (int i = _windItems.Count - 1; i >= 0; i--)
            {
                var item = _windItems[i];
                item.TimeCount += Time.deltaTime;
                if (item.TimeCount >= item.TotalTime)
                {
                    _windItems.RemoveAt(i);
                }
            }
        }
        
        private void OnVrmUnloading()
        {
            _springBones = Array.Empty<VRM10SpringBoneJoint>();
            _originalGravityDirections = Array.Empty<Vector3>();
            _originalGravityFactors = Array.Empty<float>();
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _springBones = info.instance.SpringBone.Springs.SelectMany(spring => spring.Joints).ToArray();
            _originalGravityDirections = _springBones.Select(b => b.m_gravityDir).ToArray();
            _originalGravityFactors = _springBones.Select(b => b.m_gravityPower).ToArray();
        }
    }
}
