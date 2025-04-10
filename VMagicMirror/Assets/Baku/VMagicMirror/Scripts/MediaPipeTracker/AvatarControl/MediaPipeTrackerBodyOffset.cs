﻿using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    //TODO: ITickableに書き直せそう

    /// <summary> MediaPipeのトラッキング結果を体全体の位置オフセットとして反映するやつ </summary>
    /// <remarks>
    /// 実際にトラッキングしているのは頭部の姿勢
    /// </remarks>
    public class MediaPipeTrackerBodyOffset : MonoBehaviour
    {
        private const float TweenDuration = 0.5f;
        
        [Tooltip("受け取った値の適用スケール。割と小さい方がいいかも")]
        [SerializeField] private Vector3 applyScale = new(0.3f, 0.3f, 0.3f);
        //NOTE: 移動量もフレーバー程度ということで小さめに。
        [SerializeField] private Vector3 applyMin = new(-0.05f, -0.05f, -0.02f);
        [SerializeField] private Vector3 applyMax = new(0.05f, 0.05f, 0.02f);

        //NOTE: この場合もxの比率は1.0にはせず、代わりに首回転にもとづく胴体の回転で並進が載るのに頼る
        [SerializeField] private Vector3 applyScaleWhenNoHandTrack = new(0.8f, 1f, 0.6f);
        [SerializeField] private Vector3 applyMinWhenNoHandTrack = new(-0.2f, -0.2f, -0.1f);
        [SerializeField] private Vector3 applyMaxWhenNoHandTrack = new(0.2f, 0.2f, 0.1f);
        
        [SerializeField] private float lerpFactor = 18f;
        [Tooltip("トラッキングロス時にゆっくり原点に戻すために使うLerpFactor")]
        [SerializeField] private float lerpFactorOnLost = 3f;
        [Tooltip("接続が持続しているあいだ、徐々にLerpFactorを引き上げるのに使う値")]
        [SerializeField] private float lerpFactorBlendDuration = 1f;
        
        private FaceControlConfiguration _config;
        //private ExternalTrackerDataSource _externalTracker;
        private MediaPipeKinematicSetter _mediaPipeKinematicSetter;
        
        private Vector3 _scale = Vector3.zero;
        private Vector3 _min = Vector3.zero;
        private Vector3 _max = Vector3.zero;
        private Sequence _sequence = null;
        private float _currentLerpFactor;
        
        [Inject]
        public void Initialize(FaceControlConfiguration config, MediaPipeKinematicSetter mediaPipeKinematicSetter)
        {
            _config = config;
            _mediaPipeKinematicSetter = mediaPipeKinematicSetter;
        }
        
        public Vector3 BodyOffset { get; private set; }

        private bool _noHandTrackMode = false;

        public bool NoHandTrackMode
        {
            get => _noHandTrackMode;
            set
            {
                if (_noHandTrackMode == value)
                {
                    return;
                }

                _noHandTrackMode = value;
                _sequence?.Kill();
                _sequence = DOTween.Sequence()
                    .Append(DOTween.To(
                        () => _scale,
                        v => _scale = v,
                        value ? applyScaleWhenNoHandTrack : applyScale,
                        TweenDuration
                    ))
                    .Join(DOTween.To(
                        () => _min,
                        v => _min = v,
                        value ? applyMinWhenNoHandTrack : applyMin,
                        TweenDuration
                    ))
                    .Join(DOTween.To(
                        () => _max,
                        v => _max = v,
                        value ? applyMaxWhenNoHandTrack : applyMax,
                        TweenDuration
                    ));
                _sequence.Play();
            }
            
        }

        private void Start()
        {
            //初期状態は手下げモードじゃないため、それ用のパラメータを入れておく
            _scale = applyScale;
            _min = applyMin;
            _max = applyMax;
            
            //lerpFactorは接続の信頼性に応じて上がっていく
            _currentLerpFactor = lerpFactorOnLost;
        }
        
        private void Update()
        {
            if (_config.ControlMode != FaceControlModes.WebCamHighPower)
            {
                BodyOffset = Vector3.zero;
                return;
            }

            var offset = _mediaPipeKinematicSetter.GetHeadPose().position;
            
            var goal = _mediaPipeKinematicSetter.HeadTracked
                ? new Vector3(
                    Mathf.Clamp(offset.x * _scale.x, _min.x, _max.x),
                    Mathf.Clamp(offset.y * _scale.y, _min.y, _max.y),
                    Mathf.Clamp(offset.z * _scale.z, _min.z, _max.z)
                )
                : Vector3.zero;

            if (_mediaPipeKinematicSetter.HeadTracked)
            {
                var addedLerp = 
                    _currentLerpFactor +
                    (lerpFactor - lerpFactorOnLost) * Time.deltaTime / lerpFactorBlendDuration;
                _currentLerpFactor = Mathf.Min(addedLerp, lerpFactor);
            }
            else
            {
                //ロスったら一番遅いとこまでリセットし、やり直してもらう
                _currentLerpFactor = lerpFactorOnLost;
            }
            
            BodyOffset = Vector3.Lerp(BodyOffset, goal, _currentLerpFactor * Time.deltaTime);
        }
    }
}
