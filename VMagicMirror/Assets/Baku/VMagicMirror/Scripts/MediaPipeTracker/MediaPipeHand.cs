using System;
using System.Threading;
using UnityEngine;
using Baku.VMagicMirror.IK;
using Cysharp.Threading.Tasks;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    // TODO: HandDownIkGeneratorを受け取ることで、手下げの位置をカスタムしたケースにも対応させたい
    // TODO: 指の実装が全然入ってないので入れてね
    // TODO: トラッキングロス中は手の回転をFKで決めたいが、テキトーにやってるとぜんぶIKになっちゃうので注意！
    // - FKのto-beからIK決めてもいいけど、まあそれはそれで面倒なやつ
    
    /// <summary>
    /// <see cref="BarracudaHandIK"/> の置き換えになるやつ
    /// - <see cref="HandIKIntegrator"/> では (Left|Right)Hand を参照する
    /// - Fingerの制御も何かしらいい感じにやる (はず)
    /// </summary>
    public class MediaPipeHand : PresenterBase, ITickable
    {
        private readonly IVRMLoadable _vrmLoadable;
        private readonly KinematicSetter _kinematicSetter;
        private readonly TrackingLostHandCalculator _trackingLostHandCalculator;
        private readonly MediaPipeTrackerSettingsRepository _settingsRepository;
        private readonly MediapipePoseSetterSettings _poseSetterSettings;
        private readonly CancellationTokenSource _cts = new();

        private bool _hasModel;
        private Transform _leftHandBone;
        private Transform _rightHandBone;
        // NOTE:
        // - FKやIKが完全に適用し終わったあとの値を取得してキャッシュする。
        // - トラッキングロストの計算をするときの始点に使う
        private Quaternion _leftHandLocalRotation = Quaternion.identity;
        private Quaternion _rightHandLocalRotation = Quaternion.identity;
        
        private readonly MediaPipeHandFinger _finger = new();
        private HandIkGeneratorDependency _dependency;
        private AlwaysDownHandIkGenerator _downHandIk;
        
        private bool IsInitialized => _dependency != null;

        [Inject]
        public MediaPipeHand(
            IVRMLoadable vrmLoadable,
            KinematicSetter kinematicSetter, 
            TrackingLostHandCalculator trackingLostHandCalculator,
            MediaPipeTrackerSettingsRepository settingsRepository,
            MediapipePoseSetterSettings poseSetterSettings)
        {
            _vrmLoadable = vrmLoadable;
            _kinematicSetter = kinematicSetter;
            _trackingLostHandCalculator = trackingLostHandCalculator;
            _settingsRepository = settingsRepository;
            _poseSetterSettings = poseSetterSettings;

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
        }

        private void UpdateLeftHand()
        {
            if (_kinematicSetter.TryGetLeftHandPose(out var leftHandPose))
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
            if (_kinematicSetter.TryGetRightHandPose(out var rightHandPose))
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
            public void ReleaseLeftHand()
            {
                Debug.LogError("MediaPipeの指は未反映ですよ！");
            }

            public void ReleaseRightHand()
            {
                Debug.LogError("MediaPipeの指は未反映ですよ！");
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