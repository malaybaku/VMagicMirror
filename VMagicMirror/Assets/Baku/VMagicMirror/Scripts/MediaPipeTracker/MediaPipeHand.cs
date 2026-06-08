using System;
using System.Collections;
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
        private const float ReduceSetHeadOffsetPoseFactor = 6f;

        private readonly IMessageReceiver _receiver;
        private readonly ICoroutineSource _coroutineSource;
        private readonly IVRMLoadable _vrmLoadable;
        private readonly MediaPipeKinematicSetter _mediaPipeKinematicSetter;
        private readonly TrackingLostHandCalculator _trackingLostHandCalculator;
        private readonly MediapipePoseSetterSettings _poseSetterSettings;
        private readonly CurrentFramerateChecker _framerateChecker;
        private readonly CancellationTokenSource _cts = new();
        private readonly MediaPipeHandFinger _finger;
        private readonly ReactiveProperty<float> _headPoseAdjustFactor = new(0.25f);

        private bool _hasModel;
        private Transform _vrmRoot;
        private Transform _leftHandBone;
        private Transform _rightHandBone;
        private Transform _headBone;
        // NOTE: 初期値の回転はidentityのはずなので取得せず、初期値のrootはワールド原点のはずなのでこれも取得しない
        private Vector3 _initialHeadPosition;
        private Pose _currentHeadPose;
        private Pose _currentRootPose;

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
        private bool _disposed;

        [Inject]
        public MediaPipeHand(
            IMessageReceiver receiver,
            ICoroutineSource coroutineSource,
            IVRMLoadable vrmLoadable,
            FingerController fingerController,
            MediaPipeKinematicSetter mediaPipeKinematicSetter, 
            TrackingLostHandCalculator trackingLostHandCalculator,
            MediaPipeFingerPoseCalculator fingerPoseCalculator,
            MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
            MediapipePoseSetterSettings poseSetterSettings,
            CurrentFramerateChecker framerateChecker)
        {
            _receiver = receiver;
            _coroutineSource = coroutineSource;
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

        public bool IsTracked(bool isLeft) => isLeft ? _leftHandState.IsTracked : _rightHandState.IsTracked;

        public override void Initialize()
        {
            _receiver.BindPercentageProperty(
                VmmCommands.SetHandTrackingHeadPoseAdjustFactor,
                _headPoseAdjustFactor
            );
            
            _vrmLoadable.VrmLoaded += info =>
            {
                _vrmRoot = info.vrmRoot;
                _leftHandBone = info.animator.GetBoneTransform(HumanBodyBones.LeftHand);
                _rightHandBone = info.animator.GetBoneTransform(HumanBodyBones.RightHand);
                _headBone = info.animator.GetBoneTransform(HumanBodyBones.Head);
                _initialHeadPosition = _headBone.position;
                _hasModel = true;
            };

            // モデルの読み込み直後に足元やTポーズの位置にIKが残るのを避けておく
            _vrmLoadable.PostVrmLoaded += _ =>
            {
                _leftHandState.ForceSetPosition(_downHandIk.LeftHand.Position);
                _leftHandState.SetRotation(_downHandIk.LeftHand.Rotation);
                _rightHandState.ForceSetPosition(_downHandIk.RightHand.Position);
                _rightHandState.SetRotation(_downHandIk.RightHand.Rotation);
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _vrmRoot = null;
                _leftHandBone = null;
                _rightHandBone = null;
                _headBone = null;
                _initialHeadPosition = Vector3.zero;
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

            _headPoseAdjustFactor
                .Subscribe(factor =>
                {
                    _leftHandState.HeadPoseAdjustFactor = factor;
                    _rightHandState.HeadPoseAdjustFactor = factor;
                })
                .AddTo(this);
            
            _coroutineSource.StartCoroutine(GetCurrentAvatarPoses());
        }

        public override void Dispose()
        {
            base.Dispose();
            _cts.Cancel();
            _cts.Dispose();
            _disposed = true;
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
                _leftHandState.IsTracked = false;
                _rightHandState.IsTracked = false;
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
            var isTracked =
                _mediaPipeKinematicSetter.TryGetLeftHandPose(out var handPose, out var maybeLost);
            _leftHandState.IsTracked = isTracked;

            var dt = Time.deltaTime;
            if (isTracked)
            {
                _leftHandState.SetHeadOffsetPose(GetHeadOffsetPose());
                _trackingLostHandCalculator.CancelLeftHand();

                if (maybeLost)
                {
                    // 局所的なのも含めてロストした場合: 回転は据え置きし、位置は惰性で動かす。この惰性で動くあいだはフィルタは使わない
                    _leftHandTrackedSpeed *= 1f - _poseSetterSettings.HandInertiaFactorWhenLost * dt;
                    var inertiaSpeed = 
                        Vector3.ClampMagnitude(_leftHandTrackedSpeed, _poseSetterSettings.HandMoveSpeedMax);
                    _leftHandState.ForceSetPosition(_leftHandState.PositionWithoutHeadPoseAdjust + inertiaSpeed * dt);
                }
                else
                {
                    // ロストしてなさそうな場合: フィルタベースでpos/rotを動かしつつ、ロスト時に備えて速度を記録しておく
                    var currentPosition = _leftHandState.PositionWithoutHeadPoseAdjust;
                    _leftHandState.SetFilteredPosition(
                        handPose.position,
                        _poseSetterSettings.HandMoveSpeedMax * dt
                        );
                    _leftHandTrackedSpeed = Vector3.Lerp(
                        _leftHandTrackedSpeed, 
                        (_leftHandState.PositionWithoutHeadPoseAdjust - currentPosition) / dt,
                        _poseSetterSettings.HandInertiaFactorToLogTrackedSpeed * dt
                    );

                    _leftHandState.SetRotation(Quaternion.Slerp(
                        _leftHandState.RotationWithoutHeadPoseAdjust, handPose.rotation, _poseSetterSettings.HandIkSmoothRate * dt
                    ));
                }
                
                _leftHandState.RaiseRequestToUse();
            }
            else
            {
                if (!_trackingLostHandCalculator.LeftHandTrackingLostRunning)
                {
                    _trackingLostHandCalculator.RunLeftHandTrackingLost(
                        new Pose(_leftHandState.PositionWithoutHeadPoseAdjust, _leftHandState.RotationWithoutHeadPoseAdjust), _leftHandLocalRotation
                    );
                }

                _leftHandState.ReduceSetHeadOffsetPose(dt * ReduceSetHeadOffsetPoseFactor);
                _leftHandState.ForceSetPosition(_trackingLostHandCalculator.LeftHandPose.position);
                _leftHandState.SetRotation(_trackingLostHandCalculator.LeftHandPose.rotation);

                // 完全にロストしてるケースで通過する
                _leftHandTrackedSpeed = Vector3.zero;
            }
        }

        private void UpdateRightHand()
        {
            var isTracked =
                _mediaPipeKinematicSetter.TryGetRightHandPose(out var handPose, out var maybeLost);
            _rightHandState.IsTracked = isTracked;

            var dt = Time.deltaTime;
            if (isTracked)
            {
                _rightHandState.SetHeadOffsetPose(GetHeadOffsetPose());
                _trackingLostHandCalculator.CancelRightHand();

                if (maybeLost)
                {
                    // NOTE: Vector3.zero に向けたLerpがしたいのでこう書いてる
                    _rightHandTrackedSpeed *= 1f - _poseSetterSettings.HandInertiaFactorWhenLost * dt;
                    var inertiaSpeed = 
                        Vector3.ClampMagnitude(_rightHandTrackedSpeed, _poseSetterSettings.HandMoveSpeedMax);
                    _rightHandState.ForceSetPosition(_rightHandState.PositionWithoutHeadPoseAdjust + inertiaSpeed * dt);
                }
                else
                {
                    var currentPosition = _rightHandState.PositionWithoutHeadPoseAdjust;
                    _rightHandState.SetFilteredPosition(
                        handPose.position,
                        _poseSetterSettings.HandMoveSpeedMax * dt
                    );
                    _rightHandTrackedSpeed = Vector3.Lerp(
                        _rightHandTrackedSpeed, 
                        (_rightHandState.PositionWithoutHeadPoseAdjust - currentPosition) / dt,
                        _poseSetterSettings.HandInertiaFactorToLogTrackedSpeed * dt
                    );
                    
                    _rightHandState.SetRotation(Quaternion.Slerp(
                        _rightHandState.RotationWithoutHeadPoseAdjust, handPose.rotation, _poseSetterSettings.HandIkSmoothRate * dt
                    ));
                }

                _rightHandState.RaiseRequestToUse();
            }
            else
            {
                if (!_trackingLostHandCalculator.RightHandTrackingLostRunning)
                {
                    _trackingLostHandCalculator.RunRightHandTrackingLost(
                        new Pose(_rightHandState.PositionWithoutHeadPoseAdjust, _rightHandState.RotationWithoutHeadPoseAdjust), _rightHandLocalRotation
                    );
                }

                _rightHandState.ReduceSetHeadOffsetPose(dt * ReduceSetHeadOffsetPoseFactor);
                _rightHandState.ForceSetPosition(_trackingLostHandCalculator.RightHandPose.position);
                _rightHandState.SetRotation(_trackingLostHandCalculator.RightHandPose.rotation);
                
                _rightHandTrackedSpeed = Vector3.zero;
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

        private Pose GetHeadOffsetPose()
        {
            if (!_hasModel)
            {
                return Pose.identity;
            }

            // 「rootの座標系から見てheadの位置と回転が初期値とどのくらいズレたか」を取得する
            var worldHeadPoseOffset = new Pose(
                _currentHeadPose.position - _initialHeadPosition,
                _currentHeadPose.rotation
            );
            
            return new Pose(
                Quaternion.Inverse(_currentRootPose.rotation) * worldHeadPoseOffset.position,
                Quaternion.Inverse(_currentRootPose.rotation) * worldHeadPoseOffset.rotation
            );
        }

        private IEnumerator GetCurrentAvatarPoses()
        {
            var eof = new WaitForEndOfFrame();
            while (!_disposed)
            {
                yield return eof;
                if (!_hasModel)
                {
                    continue;
                }
                
                _currentRootPose = new Pose(_vrmRoot.position, _vrmRoot.rotation);
                _currentHeadPose = new Pose(_headBone.position, _headBone.rotation);
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

            /// <summary>
            /// トラッキング中かどうか。
            /// トラッキングが瞬断しただけの(惰性で動かしてる)期間はtrueが継続し、トラッキングロスト動作で手を下げ始めるとfalse
            /// </summary>
            public bool IsTracked { get; set; }

            private float _headPoseAdjustFactor = 0f;
            public float HeadPoseAdjustFactor
            {
                get => _headPoseAdjustFactor;
                set => _headPoseAdjustFactor = Mathf.Clamp01(value);
            }
            
            public bool SkipEnterIkBlend => false;
            public MediaPipeHandFinger Finger { get; set; }

            public IKDataRecord IKData { get; } = new();

            // NOTE: CopyParameter関数が使いたいのでpublicにしてしまう
            public BiQuadFilterVector3 PositionFilter { get; } = new();

            // NOTE: 
            // - (Position|Rotation)WithoutHeadPoseAdjust はheadの動きの追従ぶんを含まず、補間の対象になる
            // - (Position|Rotation) は上記の値にhead由来の計算を加えた、「補間済みの値を追加補間無しで合成した値」になる
            //   MediaPipeHandが直接Position/Rotationをread/writeするのは期待してないのでinterface memberとしてのみ実装する
            public Vector3 PositionWithoutHeadPoseAdjust { get; private set; }
            public Quaternion RotationWithoutHeadPoseAdjust { get; private set; }
            private Pose _headOffsetPose = Pose.identity;

            Vector3 IIKData.Position => IKData.Position;
            Quaternion IIKData.Rotation => IKData.Rotation;

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
                PositionWithoutHeadPoseAdjust = value;
                UpdateIKData();
            }

            public void SetFilteredPosition(Vector3 value, float maxDistance)
            {
                var rawNextPosition = PositionFilter.Update(value);
                PositionWithoutHeadPoseAdjust = Vector3.MoveTowards(
                    PositionWithoutHeadPoseAdjust, rawNextPosition, maxDistance
                );
                UpdateIKData();
            }

            public void SetRotation(Quaternion value)
            {
                RotationWithoutHeadPoseAdjust = value;
                UpdateIKData();
            }
            
            public void SetHeadOffsetPose(Pose headOffset)
            {
                // オフセットのうち角度はヨーだけ使うので、先にここで計算しておく
                _headOffsetPose = new Pose(
                    headOffset.position, 
                    Quaternion.Euler(0f, headOffset.rotation.eulerAngles.y, 0f)
                );
                UpdateIKData();
            }

            private void UpdateIKData()
            {
                IKData.Position = Vector3.Lerp(
                    PositionWithoutHeadPoseAdjust,
                    _headOffsetPose.rotation * PositionWithoutHeadPoseAdjust + _headOffsetPose.position,
                    HeadPoseAdjustFactor
                );

                IKData.Rotation = Quaternion.Slerp(
                    RotationWithoutHeadPoseAdjust,
                    _headOffsetPose.rotation * RotationWithoutHeadPoseAdjust,
                    HeadPoseAdjustFactor
                );
            }

            public void ReduceSetHeadOffsetPose(float factor)
            {
                SetHeadOffsetPose(new Pose(
                    _headOffsetPose.position * (1 - factor),
                    Quaternion.Slerp(_headOffsetPose.rotation, Quaternion.identity, 1 - factor)
                ));
            }
        }
    }
}