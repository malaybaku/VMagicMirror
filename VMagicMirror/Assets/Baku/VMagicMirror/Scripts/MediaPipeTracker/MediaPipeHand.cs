using System;
using System.Threading;
using UnityEngine;
using Baku.VMagicMirror.IK;
using Cysharp.Threading.Tasks;
using Zenject;
using R3;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    /// <summary>
    /// ハンドトラッキングの結果をアバターに適用可能なIK情報に変換するやつ。
    /// - <see cref="HandIKIntegrator"/> では (Left|Right)Hand を参照する
    /// - Fingerの制御も何かしらいい感じにやる
    /// </summary>
    public class MediaPipeHand : PresenterBase, ITickable
    {
        private const float HandPositionCutoffFrequency = 5f;

        private readonly IVRMLoadable _vrmLoadable;
        private readonly MediaPipeKinematicSetter _mediaPipeKinematicSetter;
        private readonly TrackingLostHandCalculator _trackingLostHandCalculator;
        private readonly MediapipePoseSetterSettings _poseSetterSettings;
        private readonly CurrentFramerateChecker _framerateChecker;
        private readonly CancellationTokenSource _cts = new();
        private readonly MediaPipeHandFinger _finger;

        private bool _hasModel;
        private Transform _leftHandBone;
        private Transform _rightHandBone;
        // NOTE:
        // - FKやIKが完全に適用し終わったあとの値を取得してキャッシュする。
        // - トラッキングロストの計算をするときの始点に使う
        private Quaternion _leftHandLocalRotation = Quaternion.identity;
        private Quaternion _rightHandLocalRotation = Quaternion.identity;

        // NOTE: 下記の速度はトラッキングロスト状態に入るとゼロに戻る
        private Vector3 _leftHandTrackedSpeed = Vector3.zero;
        private Vector3 _rightHandTrackedSpeed = Vector3.zero;
        
        private HandIkGeneratorDependency _dependency;
        private AlwaysDownHandIkGenerator _downHandIk;
        
        private bool IsInitialized => _dependency != null;

        [Inject]
        public MediaPipeHand(
            IVRMLoadable vrmLoadable,
            FingerController fingerController,
            MediaPipeKinematicSetter mediaPipeKinematicSetter, 
            TrackingLostHandCalculator trackingLostHandCalculator,
            MediaPipeFingerPoseCalculator fingerPoseCalculator,
            MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
            MediapipePoseSetterSettings poseSetterSettings,
            CurrentFramerateChecker framerateChecker)
        {
            _vrmLoadable = vrmLoadable;
            _mediaPipeKinematicSetter = mediaPipeKinematicSetter;
            _trackingLostHandCalculator = trackingLostHandCalculator;
            _poseSetterSettings = poseSetterSettings;
            _framerateChecker = framerateChecker;

            // NOTE: 指の操作はFingerControllerに委譲されている
            _finger = new MediaPipeHandFinger(settingsRepository, fingerPoseCalculator, fingerController);
            _leftHandState = new MediaPipeHandState(ReactedHand.Left, _finger);
            _rightHandState = new MediaPipeHandState(ReactedHand.Right, _finger);
        }


        private readonly MediaPipeHandState _leftHandState;
        public IHandIkState LeftHandState => _leftHandState;

        private readonly MediaPipeHandState _rightHandState;
        public IHandIkState RightHandState => _rightHandState;

        public void SetDependency(HandIkGeneratorDependency dependency, AlwaysDownHandIkGenerator downHandIk)
        {
            _dependency = dependency;
            _downHandIk = downHandIk;
            _trackingLostHandCalculator.SetupDownHandIk(downHandIk);
        }

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += info =>
            {
                _leftHandBone = info.animator.GetBoneTransform(HumanBodyBones.LeftHand);
                _rightHandBone = info.animator.GetBoneTransform(HumanBodyBones.RightHand);
                _hasModel = true;
            };

            // モデルの読み込み直後に足元やTポーズの位置にIKが残るのを避けておく
            _vrmLoadable.PostVrmLoaded += _ =>
            {
                _leftHandState.ForceSetPosition(_downHandIk.LeftHand.Position);
                _leftHandState.Rotation = _downHandIk.LeftHand.Rotation;
                _rightHandState.ForceSetPosition(_downHandIk.RightHand.Position);
                _rightHandState.Rotation = _downHandIk.RightHand.Rotation;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _leftHandBone = null;
                _rightHandBone = null;
            };

            CheckHandLocalRotationAsync(_cts.Token).Forget();

            _trackingLostHandCalculator.LeftHandTrackingLostMotionCompleted
                .Subscribe(_ => ResetLeftHandFingerOnTrackingLost())
                .AddTo(this);

            _trackingLostHandCalculator.RightHandTrackingLostMotionCompleted
                .Subscribe(_ => ResetRightHandFingerOnTrackingLost())
                .AddTo(this);

            _framerateChecker.CurrentFramerate
                .Subscribe(SetupFilters)
                .AddTo(this);
        }

        public override void Dispose()
        {
            base.Dispose();
            _cts.Cancel();
            _cts.Dispose();
        }

        private async UniTaskVoid CheckHandLocalRotationAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // NOTE: 姿勢が完全に確定し終わったあとの結果が知りたいので、このタイミングで取る
                await UniTask.NextFrame(PlayerLoopTiming.LastPostLateUpdate, cancellationToken: cancellationToken);
                if (_hasModel)
                {
                    _leftHandLocalRotation = _leftHandBone.localRotation;
                    _rightHandLocalRotation = _rightHandBone.localRotation;
                }
                else
                {
                    _leftHandLocalRotation = Quaternion.identity;
                    _rightHandLocalRotation = Quaternion.identity;
                }
            }
        }

        // NOTE: HandIkIntegratorにUpdate/LateUpdateを呼ばせるスタイルにしてもよいかも。タイミングの都合次第になる
        void ITickable.Tick()
        {
            // NOTE:
            // - IsInitializedは大体つねにtrueなのでガードする意義は薄め
            // - トラッキングロストの動作が始まった後は「手下げたまま」状態になることがあり、それを止める理由はないので流しっぱなしにする
            if (!IsInitialized || !_hasModel)
            {
                return;
            }
            
            UpdateLeftHand();
            UpdateRightHand();
            if (_dependency.Config.LeftTarget.CurrentValue is HandTargetType.ImageBaseHand)
            {
                _finger.ApplyLeftHandFinger();
            }
            
            if (_dependency.Config.RightTarget.CurrentValue is HandTargetType.ImageBaseHand)
            {
                _finger.ApplyRightHandFinger();
            }
        }

        private void SetupFilters(float framerate)
        {
            _leftHandState.PositionFilter.SetUpAsLowPassFilter(framerate, HandPositionCutoffFrequency);
            _rightHandState.PositionFilter.CopyParametersFrom(_leftHandState.PositionFilter);
            _finger.SetupFilters(framerate);
        }
        
        private void UpdateLeftHand()
        {
            if (_mediaPipeKinematicSetter.TryGetLeftHandPose(out var handPose, out var maybeLost))
            {
                _trackingLostHandCalculator.CancelLeftHand();
                var dt = Time.deltaTime;

                if (maybeLost)
                {
                    // 局所的なのも含めてロストした場合: 回転は据え置きし、位置は惰性で動かす。この惰性で動くあいだはフィルタは使わない
                    _leftHandTrackedSpeed *= 1f - _poseSetterSettings.HandInertiaFactorWhenLost * dt;
                    var inertiaSpeed = 
                        Vector3.ClampMagnitude(_leftHandTrackedSpeed, _poseSetterSettings.HandMoveSpeedMax);
                    _leftHandState.ForceSetPosition(_leftHandState.Position + inertiaSpeed * dt);
                }
                else
                {
                    // ロストしてなさそうな場合: フィルタベースでpos/rotを動かしつつ、ロスト時に備えて速度を記録しておく
                    var currentPosition = _leftHandState.Position;
                    _leftHandState.SetFilteredPosition(
                        handPose.position,
                        _poseSetterSettings.HandMoveSpeedMax * dt
                        );
                    _leftHandTrackedSpeed = Vector3.Lerp(
                        _leftHandTrackedSpeed, 
                        (_leftHandState.Position - currentPosition) / dt,
                        _poseSetterSettings.HandInertiaFactorToLogTrackedSpeed * dt
                    );
                    
                    _leftHandState.Rotation = Quaternion.Slerp(
                        _leftHandState.Rotation, handPose.rotation, _poseSetterSettings.HandIkSmoothRate * dt
                    );
                }
                
                _leftHandState.RaiseRequestToUse();
            }
            else
            {
                if (!_trackingLostHandCalculator.LeftHandTrackingLostRunning)
                {
                    _trackingLostHandCalculator.RunLeftHandTrackingLost(
                        new Pose(_leftHandState.Position, _leftHandState.Rotation), _leftHandLocalRotation
                    );
                }

                _leftHandState.ForceSetPosition(_trackingLostHandCalculator.LeftHandPose.position);
                _leftHandState.Rotation = _trackingLostHandCalculator.LeftHandPose.rotation;
                
                // 完全にロストしてるケースで通過する
                _leftHandTrackedSpeed = Vector3.zero;
            }
        }

        private void UpdateRightHand()
        {
            if (_mediaPipeKinematicSetter.TryGetRightHandPose(out var handPose, out var maybeLost))
            {
                _trackingLostHandCalculator.CancelRightHand();
                var dt = Time.deltaTime;

                if (maybeLost)
                {
                    // NOTE: Vector3.zero に向けたLerpがしたいのでこう書いてる
                    _rightHandTrackedSpeed *= 1f - _poseSetterSettings.HandInertiaFactorWhenLost * dt;
                    var inertiaSpeed = 
                        Vector3.ClampMagnitude(_rightHandTrackedSpeed, _poseSetterSettings.HandMoveSpeedMax);
                    _rightHandState.ForceSetPosition(_rightHandState.Position + inertiaSpeed * dt);
                }
                else
                {
                    var currentPosition = _rightHandState.Position;
                    _rightHandState.SetFilteredPosition(
                        handPose.position,
                        _poseSetterSettings.HandMoveSpeedMax * dt
                    );
                    _rightHandTrackedSpeed = Vector3.Lerp(
                        _rightHandTrackedSpeed, 
                        (_rightHandState.Position - currentPosition) / dt,
                        _poseSetterSettings.HandInertiaFactorToLogTrackedSpeed * dt
                    );
                    
                    _rightHandState.Rotation = Quaternion.Slerp(
                        _rightHandState.Rotation, handPose.rotation, _poseSetterSettings.HandIkSmoothRate * dt
                    );
                }

                _rightHandState.RaiseRequestToUse();
            }
            else
            {
                if (!_trackingLostHandCalculator.RightHandTrackingLostRunning)
                {
                    _trackingLostHandCalculator.RunRightHandTrackingLost(
                        new Pose(_rightHandState.Position, _rightHandState.Rotation), _rightHandLocalRotation
                    );
                }

                _rightHandState.ForceSetPosition(_trackingLostHandCalculator.RightHandPose.position);
                _rightHandState.Rotation = _trackingLostHandCalculator.RightHandPose.rotation;
            }
        }
        
        private void ResetLeftHandFingerOnTrackingLost()
        {
            // NOTE: トラッキングロスト→別の手IKに移行→トラッキングロスト動作完了、みたいなケースがあるのでTargetのチェックが必要
            if (_dependency.Config.LeftTarget.CurrentValue is HandTargetType.ImageBaseHand)
            {
                _finger.ReleaseLeftHand();
                _finger.ResetCalculatedLeftFingerPoses();
            }
        }
        
        private void ResetRightHandFingerOnTrackingLost()
        {
            if (_dependency.Config.RightTarget.CurrentValue is HandTargetType.ImageBaseHand)
            {
                _finger.ReleaseRightHand();
                _finger.ResetCalculatedRightFingerPoses();
            }
        }
        
        private class MediaPipeHandFinger
        {
            private const float FingerAngleMoveCutoffFrequency = 4f;

            private readonly MediaPipeTrackerRuntimeSettingsRepository _settingsRepository;
            private readonly MediaPipeFingerPoseCalculator _fingerPoseCalculator;
            private readonly FingerController _fingerController;

            private readonly BiQuadFilter[] _bendAngleFilters = new BiQuadFilter[10];
            private readonly BiQuadFilter[] _openAngleFilters = new BiQuadFilter[10];

            public MediaPipeHandFinger(
                MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
                MediaPipeFingerPoseCalculator fingerPoseCalculator,
                FingerController fingerController
                )
            {
                _settingsRepository = settingsRepository;
                _fingerPoseCalculator = fingerPoseCalculator;
                _fingerController = fingerController;
                for (var i = 0; i < 10; i++)
                {
                    _bendAngleFilters[i] = new BiQuadFilter();
                    _openAngleFilters[i] = new BiQuadFilter();
                }
                SetupFilters(60f);
            }

            public void SetupFilters(float framerate)
            {
                _bendAngleFilters[0].SetUpAsLowPassFilter(framerate, FingerAngleMoveCutoffFrequency);

                var referenceFilter = _bendAngleFilters[0];
                _openAngleFilters[0].CopyParametersFrom(referenceFilter);
                for (var i = 1; i < _bendAngleFilters.Length; i++)
                {
                    _bendAngleFilters[i].CopyParametersFrom(referenceFilter);
                    _openAngleFilters[i].CopyParametersFrom(referenceFilter);
                }
            }
            
            public void ApplyLeftHandFinger() => ApplyFingerAngles(true);
            public void ApplyRightHandFinger() => ApplyFingerAngles(false);

            private void ApplyFingerAngles(bool isLeft)
            {
                var mirrored = _settingsRepository.IsHandMirrored.Value;
                if (!CanHoldFinger(isLeft, mirrored))
                {
                    return;
                }
                
                var thumbIndex = isLeft ? FingerConsts.LeftThumb : FingerConsts.RightThumb;
                var littleIndex = isLeft ? FingerConsts.LeftLittle : FingerConsts.RightLittle;
                for (var i = thumbIndex; i <= littleIndex; i++)
                {
                    var angles = _fingerPoseCalculator.GetFingerAngles(i, mirrored);
                    var rawBendAngle = GetFingerBendAngle(angles.proximal, angles.intermediate, angles.distal);
                    var bendAngle = _bendAngleFilters[i].Update(rawBendAngle);
                    var openAngle = _openAngleFilters[i].Update(angles.open);
                    
                    _fingerController.Hold(i, bendAngle);
                    // NOTE: 親指のopenの制御がキモくなりそうな場合、親指だけここをスキップすべき
                    _fingerController.HoldOpen(i, openAngle);
                }
            }

            // NOTE: トラッキング値の受信前やトラッキングロスト後にHoldを呼ばないためのガード関数
            private bool CanHoldFinger(bool isLeft, bool mirrored)
            {
                if (mirrored)
                {
                    isLeft = !isLeft;
                }

                if ((isLeft && !_fingerPoseCalculator.LeftHandPoseHasValidValue) ||
                    (!isLeft && !_fingerPoseCalculator.RightHandPoseHasValidValue))
                {
                    return false;
                }

                return true;
            }
            
            // NOTE: ちょっともったいないが、曲げ角度を平均して適用する
            private static float GetFingerBendAngle(float proximal, float intermediate, float distal)
                => (proximal + intermediate + distal) / 3.0f;

            public void ReleaseLeftHand()
            {
                for (var i = FingerConsts.LeftThumb; i < FingerConsts.LeftLittle + 1; i++)
                {
                    _fingerController.Release(i);
                    _fingerController.ReleaseOpen(i);
                    _bendAngleFilters[i].ResetValue(0f);
                    _openAngleFilters[i].ResetValue(0f);
                }
            }
        
            public void ReleaseRightHand()
            {
                for (var i = FingerConsts.RightThumb; i < FingerConsts.RightLittle + 1; i++)
                {
                    _fingerController.Release(i);
                    _fingerController.ReleaseOpen(i);
                    _bendAngleFilters[i].ResetValue(0f);
                    _openAngleFilters[i].ResetValue(0f);
                }
            }

            public void ResetCalculatedLeftFingerPoses()
            {
                var mirrored = _settingsRepository.IsHandMirrored.Value;
                if (mirrored)
                {
                    _fingerPoseCalculator.ResetRightHandPose();
                }
                else
                {
                    _fingerPoseCalculator.ResetLeftHandPose();
                }
            }
            
            public void ResetCalculatedRightFingerPoses()
            {
                var mirrored = _settingsRepository.IsHandMirrored.Value;
                if (mirrored)
                {
                    _fingerPoseCalculator.ResetLeftHandPose();
                }
                else
                {
                    _fingerPoseCalculator.ResetRightHandPose();
                }
            }
        }

        private class MediaPipeHandState : IHandIkState
        {
            public MediaPipeHandState(ReactedHand hand, MediaPipeHandFinger finger)
            {
                Hand = hand;
                Finger = finger;
            }

            public bool SkipEnterIkBlend => false;
            public MediaPipeHandFinger Finger { get; set; }

            public IKDataRecord IKData { get; } = new();

            // NOTE: CopyParameter関数が使いたいのでpublicにしてしまう
            public BiQuadFilterVector3 PositionFilter { get; } = new();

            public Vector3 Position => IKData.Position;

            public Quaternion Rotation
            {
                get => IKData.Rotation;
                set => IKData.Rotation = value;
            }

            public ReactedHand Hand { get; }
            public HandTargetType TargetType => HandTargetType.ImageBaseHand;

            public void RaiseRequestToUse() => RequestToUse?.Invoke(this);
            public event Action<IHandIkState> RequestToUse;

            public event Action<ReactedHand, IHandIkState> OnEnter;
            public event Action<ReactedHand> OnQuit;

            public void Enter(IHandIkState prevState) => OnEnter?.Invoke(Hand, prevState);

            public void Quit(IHandIkState nextState)
            {
                if (Hand == ReactedHand.Left)
                {
                    Finger?.ReleaseLeftHand();
                }
                else
                {
                    Finger?.ReleaseRightHand();
                }

                OnQuit?.Invoke(Hand);
            }

            public void ForceSetPosition(Vector3 value)
            {
                PositionFilter.ResetValue(value);
                IKData.Position = value;
            }

            public void SetFilteredPosition(Vector3 value, float maxDistance)
            {
                var rawNextPosition = PositionFilter.Update(value);
                IKData.Position = Vector3.MoveTowards(
                    IKData.Position, rawNextPosition, maxDistance
                );
            }
        }
    }
}