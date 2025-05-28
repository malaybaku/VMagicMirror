﻿using Baku.VMagicMirror.ExternalTracker;
using DG.Tweening;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 外部トラッキングアプリがXYZオフセットをサポートしている場合に
    /// そのオフセットを体に効かせる処理です。
    /// </summary>
    public class ExternalTrackerBodyOffset : MonoBehaviour
    {
        private const float TweenDuration = 0.5f;
        
        [Tooltip("受け取った値の適用スケール。割と小さい方がいいかも")]
        [SerializeField] private Vector3 applyScale = new Vector3(0.3f, 0.3f, 0.3f);
        //NOTE: 移動量もフレーバー程度ということで小さめに。
        [SerializeField] private Vector3 applyMin = new Vector3(-0.05f, -0.05f, -0.02f);
        [SerializeField] private Vector3 applyMax = new Vector3(0.05f, 0.05f, 0.02f);

        //NOTE: この場合もxの比率は1.0にはせず、代わりに首回転にもとづく胴体の回転で並進が載るのに頼る
        [SerializeField] private Vector3 applyScaleWhenNoHandTrack = new Vector3(0.8f, 1f, 0.6f);
        [SerializeField] private Vector3 applyMinWhenNoHandTrack = new Vector3(-0.2f, -0.2f, -0.1f);
        [SerializeField] private Vector3 applyMaxWhenNoHandTrack = new Vector3(0.2f, 0.2f, 0.1f);
        
        [SerializeField] private float lerpFactor = 18f;
        [Tooltip("トラッキングロス時にゆっくり原点に戻すために使うLerpFactor")]
        [SerializeField] private float lerpFactorOnLost = 3f;
        [Tooltip("接続が持続しているあいだ、徐々にLerpFactorを引き上げるのに使う値")]
        [SerializeField] private float lerpFactorBlendDuration = 1f;
        
        private FaceControlConfiguration _config;
        private ExternalTrackerDataSource _externalTracker;
        
        private Vector3 _scale = Vector3.zero;
        private Vector3 _min = Vector3.zero;
        private Vector3 _max = Vector3.zero;
        private Sequence _sequence = null;
        private float _currentLerpFactor;

        // UI上で「手前・奥に移動」がオンかどうか
        private readonly ReactiveProperty<bool> _preferMoveZ = new(false);

        [Inject]
        public void Initialize(
            IMessageReceiver receiver,
            FaceControlConfiguration config,
            ExternalTrackerDataSource externalTracker)
        {
            _config = config;
            _externalTracker = externalTracker;
            
            receiver.BindBoolProperty(VmmCommands.EnableBodyLeanZ, _preferMoveZ);

            NoHandTrackMode
                .CombineLatest(
                    _preferMoveZ,
                    (noHandTrackMode, preferMoveZ) =>
                        (noHandTrackMode, noHandTrackMode && preferMoveZ)
                    )
                .DistinctUntilChanged()
                .Subscribe(value => SetScaleMinMaxTarget(value.noHandTrackMode, value.Item2))
                .AddTo(this);
        }
        
        public Vector3 BodyOffset { get; private set; }

        public ReactiveProperty<bool> NoHandTrackMode { get; } = new(false);

        private void SetScaleMinMaxTarget(bool noHandTrackMode, bool applyLargeOffsetZ)
        {
            var targetScale = noHandTrackMode ? applyScaleWhenNoHandTrack : applyScale;
            var targetMin = noHandTrackMode ? applyMinWhenNoHandTrack : applyMin;
            var targetMax = noHandTrackMode ? applyMaxWhenNoHandTrack : applyMax;

            // 手下げモードの場合に、前後の移動は大きいかどうかは追加オプションでジャッジされる
            if (noHandTrackMode && !applyLargeOffsetZ)
            {
                targetScale.z = applyScale.z;
                targetMin.z = applyMin.z;
                targetMax.z = applyMax.z;
            }
            
            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(DOTween.To(
                    () => _scale, v => _scale = v, targetScale, TweenDuration
                ))
                .Join(DOTween.To(
                    () => _min, v => _min = v, targetMin, TweenDuration
                ))
                .Join(DOTween.To(
                    () => _max, v => _max = v, targetMax, TweenDuration
                ));
            _sequence.Play();
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
            if (_config.ControlMode != FaceControlModes.ExternalTracker ||
                !_externalTracker.SupportFacePositionOffset)
            {
                BodyOffset = Vector3.zero;
                return;
            }
            
            var offset = _externalTracker.HeadPositionOffset;
            
            var goal = _externalTracker.Connected
                ? new Vector3(
                    Mathf.Clamp(offset.x * _scale.x, _min.x, _max.x),
                    Mathf.Clamp(offset.y * _scale.y, _min.y, _max.y),
                    Mathf.Clamp(offset.z * _scale.z, _min.z, _max.z)
                )
                : Vector3.zero;

            if (_externalTracker.Connected)
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
