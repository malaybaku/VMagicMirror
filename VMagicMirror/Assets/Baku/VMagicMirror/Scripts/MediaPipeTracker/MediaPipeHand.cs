using System;
using System.Threading;
using UnityEngine;
using Baku.VMagicMirror.IK;
using Cysharp.Threading.Tasks;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // TODO: トラッキングロス中は手の回転をFKで決めたいが、テキトーにやってるとぜんぶIKになっちゃうので注意！
    // - FKのto-beからIK決めてもいいけど、まあそれはそれで面倒なやつ
    
    /// <summary>
    /// ハンドトラッキングの結果をアバターに適用可能なIK情報に変換するやつ。
    /// - <see cref="HandIKIntegrator"/> では (Left|Right)Hand を参照する
    /// - Fingerの制御も何かしらいい感じにやる
    /// </summary>
    public class MediaPipeHand : PresenterBase, ITickable
    {
        private readonly IVRMLoadable _vrmLoadable;
        private readonly MediaPipeKinematicSetter _mediaPipeKinematicSetter;
        private readonly TrackingLostHandCalculator _trackingLostHandCalculator;
        private readonly MediapipePoseSetterSettings _poseSetterSettings;
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
            MediapipePoseSetterSettings poseSetterSettings)
        {
            _vrmLoadable = vrmLoadable;
            _mediaPipeKinematicSetter = mediaPipeKinematicSetter;
            _trackingLostHandCalculator = trackingLostHandCalculator;
            _poseSetterSettings = poseSetterSettings;

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
                _leftHandState.Position = _downHandIk.LeftHand.Position;
                _leftHandState.Rotation = _downHandIk.LeftHand.Rotation;
                _rightHandState.Position = _downHandIk.RightHand.Position;
                _rightHandState.Rotation = _downHandIk.RightHand.Rotation;
            };

            _vrmLoadable.VrmDisposing += () =>
            {
                _hasModel = false;
                _leftHandBone = null;
                _rightHandBone = null;
            };

            CheckHandLocalRotationAsync(_cts.Token).Forget();
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
            if (_dependency.Config.LeftTarget.Value is HandTargetType.ImageBaseHand)
            {
                _finger.ApplyLeftHandFinger();
            }
            
            if (_dependency.Config.RightTarget.Value is HandTargetType.ImageBaseHand)
            {
                _finger.ApplyRightHandFinger();
            }
        }

        private void UpdateLeftHand()
        {
            if (_mediaPipeKinematicSetter.TryGetLeftHandPose(out var leftHandPose))
            {
                _trackingLostHandCalculator.CancelLeftHand();

                var dt = Time.deltaTime;
                var nextPos = Vector3.Lerp(
                    _leftHandState.Position, leftHandPose.position, _poseSetterSettings.HandIkSmoothRate * dt
                );
                _leftHandState.Position = Vector3.MoveTowards(
                    _leftHandState.Position, nextPos, _poseSetterSettings.HandMoveSpeedMax * dt
                );
                _leftHandState.Rotation = Quaternion.Slerp(
                    _leftHandState.Rotation, leftHandPose.rotation, _poseSetterSettings.HandIkSmoothRate * dt
                );
                
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

                _leftHandState.Position = _trackingLostHandCalculator.LeftHandPose.position;
                _leftHandState.Rotation = _trackingLostHandCalculator.LeftHandPose.rotation;
            }
        }

        private void UpdateRightHand()
        {
            if (_mediaPipeKinematicSetter.TryGetRightHandPose(out var rightHandPose))
            {
                _trackingLostHandCalculator.CancelRightHand();

                var dt = Time.deltaTime;
                var nextPos = Vector3.Lerp(
                    _rightHandState.Position, rightHandPose.position, _poseSetterSettings.HandIkSmoothRate * dt
                );
                _rightHandState.Position = Vector3.MoveTowards(
                    _rightHandState.Position, nextPos, _poseSetterSettings.HandMoveSpeedMax * dt
                );
                _rightHandState.Rotation = Quaternion.Slerp(
                    _rightHandState.Rotation, rightHandPose.rotation, _poseSetterSettings.HandIkSmoothRate * dt
                );

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

                _rightHandState.Position = _trackingLostHandCalculator.RightHandPose.position;
                _rightHandState.Rotation = _trackingLostHandCalculator.RightHandPose.rotation;
            }
        }
        
        private class MediaPipeHandFinger
        {
            private readonly MediaPipeTrackerRuntimeSettingsRepository _settingsRepository;
            private readonly MediaPipeFingerPoseCalculator _fingerPoseCalculator;
            private readonly FingerController _fingerController;

            public MediaPipeHandFinger(
                MediaPipeTrackerRuntimeSettingsRepository settingsRepository,
                MediaPipeFingerPoseCalculator fingerPoseCalculator,
                FingerController fingerController
                )
            {
                _settingsRepository = settingsRepository;
                _fingerPoseCalculator = fingerPoseCalculator;
                _fingerController = fingerController;
            }

            public void ApplyLeftHandFinger()
                => ApplyFingerAngles(FingerConsts.LeftThumb, FingerConsts.LeftLittle);

            public void ApplyRightHandFinger() 
                => ApplyFingerAngles(FingerConsts.RightThumb, FingerConsts.RightLittle);

            private void ApplyFingerAngles(int thumbIndex, int littleIndex)
            {
                var mirrored = _settingsRepository.IsHandMirrored.Value;
                for (var i = thumbIndex; i <= littleIndex; i++)
                {
                    var angles = _fingerPoseCalculator.GetFingerAngles(i, mirrored);
                    
                    _fingerController.Hold(i, GetFingerBendAngle(
                        angles.proximal, angles.intermediate, angles.distal
                    ));
                    // NOTE: 親指のopenの制御がキモくなりそうな場合、親指だけここをスキップすべき
                    _fingerController.HoldOpen(i, angles.open);
                }
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
                }
            }
        
            public void ReleaseRightHand()
            {
                for (var i = FingerConsts.RightThumb; i < FingerConsts.RightLittle + 1; i++)
                {
                    _fingerController.Release(i);
                    _fingerController.ReleaseOpen(i);
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

            public Vector3 Position
            {
                get => IKData.Position;
                set => IKData.Position = value;
            }

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
        }
    }
}