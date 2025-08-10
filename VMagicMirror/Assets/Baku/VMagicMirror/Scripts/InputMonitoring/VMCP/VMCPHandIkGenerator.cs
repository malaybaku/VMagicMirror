using System;
using Baku.VMagicMirror.IK;
using R3;
using UnityEngine;

namespace Baku.VMagicMirror.VMCP
{
    public class VMCPHandIkGenerator : HandIkGeneratorBase
    {
        // VMCPが未受信になると0.5secで手を下ろす
        // NOTE: 「未受信」はBarracudaHandとかのトラッキングロスよりは起きにくいのであまり凝った処理はしない。
        // 送信元自体がトラッキングロスすることはあるが、その場合のモーション補間は送信元が頑張ってるはずなので信じる
        private const float ConnectedBlendRate = 2f;
        private const float RaiseRequestInterval = 0.2f;

        public VMCPHandIkGenerator(
            HandIkGeneratorDependency dependency, 
            VMCPHandPose vmcpHandPose, 
            VMCPFingerController fingerController,
            AlwaysDownHandIkGenerator downHand) 
            : base(dependency)
        {
            _vmcpHandPose = vmcpHandPose;
            _fingerController = fingerController;
            _leftHandState = new VMCPHandIkState(ReactedHand.Left, downHand.LeftHand);
            _rightHandState = new VMCPHandIkState(ReactedHand.Right, downHand.RightHand);

            //NOTE: 定期的にリクエストを飛ばすのは手下げモードのon/offの切り替わりに配慮している
            vmcpHandPose.IsActive
                .Subscribe(active =>
                {
                    if (active)
                    {
                        _raiseRequestDisposable.Disposable = Observable
                            .Timer(TimeSpan.Zero, TimeSpan.FromSeconds(RaiseRequestInterval))
                            .Subscribe(_ =>
                            {
                                _leftHandState.RaiseRequest();
                                _rightHandState.RaiseRequest();
                            });
                    }
                    else
                    {
                        _raiseRequestDisposable.Disposable = Disposable.Empty;
                    }
                })
                .AddTo(dependency.Component);

            vmcpHandPose.LeftHandPose
                .Subscribe(pose => _leftHandState.SetRawPose(pose.Position, pose.Rotation))
                .AddTo(dependency.Component);
            
            vmcpHandPose.RightHandPose
                .Subscribe(pose => _rightHandState.SetRawPose(pose.Position, pose.Rotation))
                .AddTo(dependency.Component);

            _fingerController.SetLateUpdateCallback(LateUpdateCallback);
        }

        public override void Update()
        {
            if (_vmcpHandPose.IsActive.Value)
            {
                var diff = Time.deltaTime * ConnectedBlendRate;
                _connectedRate =
                    Mathf.Clamp01(_vmcpHandPose.IsConnected.Value ? _connectedRate + diff : _connectedRate - diff);
                var rate = Mathf.SmoothStep(0, 1, _connectedRate);
                _leftHandState.ApplyWithRate(rate);
                _rightHandState.ApplyWithRate(rate);
            }
            else
            {
                _connectedRate = 0f;
            }
        }

        public override void Dispose()
        {
            _raiseRequestDisposable.Dispose();
        }

        private void LateUpdateCallback()
        {
            //指を適用する: FingerController経由じゃないことには注意
            if (_vmcpHandPose.IsActive.Value)
            {
                _vmcpHandPose.ApplyFingerLocalPose();
            }
        }
        
        private readonly VMCPHandPose _vmcpHandPose;
        private readonly VMCPFingerController _fingerController;
        private readonly VMCPHandIkState _leftHandState;
        public override IHandIkState LeftHandState => _leftHandState;
        private readonly VMCPHandIkState _rightHandState;
        public override IHandIkState RightHandState => _rightHandState;
        public ReadOnlyReactiveProperty<bool> IsActive => _vmcpHandPose.IsActive;

        private readonly SerialDisposable _raiseRequestDisposable = new SerialDisposable();
        private float _connectedRate = 0f;

        class VMCPHandIkState : IHandIkState
        {
            public VMCPHandIkState(ReactedHand hand, IIKData downHand)
            {
                Hand = hand;
                _downHand = downHand;
            }

            public void SetRawPose(Vector3 position, Quaternion rotation)
            {
                _rawPosition = position;
                _rawRotation = rotation;
            }

            public void ApplyWithRate(float rate)
            {
                Position = Vector3.Lerp(_downHand.Position, _rawPosition, rate);
                Rotation = Quaternion.Slerp(_downHand.Rotation, _rawRotation, rate);
            }

            private readonly IIKData _downHand;
            private Vector3 _rawPosition;
            private Quaternion _rawRotation = Quaternion.identity;

            public Vector3 Position { get; private set; }
            public Quaternion Rotation { get; private set; } = Quaternion.identity;
            public bool SkipEnterIkBlend => false;
            public ReactedHand Hand { get; }
            public HandTargetType TargetType => HandTargetType.VMCPReceiveResult;

            public void RaiseRequest() => RequestToUse?.Invoke(this);
            public event Action<IHandIkState> RequestToUse;

            //NOTE: 遷移元を覚えておき、VMCPがオフになったときに遷移させるようにしてもよい
            //そうじゃない場合、HandIkIntegratorにGODをやってもらうということで…
            public void Enter(IHandIkState prevState)
            {
            }
            
            public void Quit(IHandIkState nextState)
            {
            }
        }
    }
}