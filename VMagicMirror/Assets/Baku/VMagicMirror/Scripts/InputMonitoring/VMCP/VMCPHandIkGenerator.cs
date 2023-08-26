using System;
using Baku.VMagicMirror.IK;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.VMCP
{
    public class VMCPHandIkGenerator : HandIkGeneratorBase
    {
        // VMCPが未受信になると0.5secで手を下ろす
        // NOTE: 「未受信」はBarracudaHandとかのトラッキングロスよりは起きにくいのであまり凝った処理はしない。
        // 送信元自体がトラッキングロスすることはあるが、その場合のモーション補間は送信元が頑張ってるはずなので信じる
        private const float ConnectedBlendRate = 2f;
        
        public VMCPHandIkGenerator(
            HandIkGeneratorDependency dependency, VMCPHandPose vmcpHandPose, AlwaysDownHandIkGenerator downHand) 
            : base(dependency)
        {
            _vmcpHandPose = vmcpHandPose;
            _leftHandState = new VMCPHandIkState(ReactedHand.Left, downHand.LeftHand);
            _rightHandState = new VMCPHandIkState(ReactedHand.Right, downHand.RightHand);

            //オフ -> オンへの切り替え時だけリクエストが出るが、それ以上にバシバシ送ったほうが良ければ頻度を上げてもOK
            vmcpHandPose.IsActive
                .Subscribe(active =>
                {
                    if (active)
                    {
                        _leftHandState.RaiseRequest();
                        _rightHandState.RaiseRequest();
                    }                    
                })
                .AddTo(dependency.Component);

            vmcpHandPose.LeftHandPose
                .Subscribe(pose => _leftHandState.SetRawPose(pose.Position, pose.Rotation))
                .AddTo(dependency.Component);
            
            vmcpHandPose.RightHandPose
                .Subscribe(pose => _rightHandState.SetRawPose(pose.Position, pose.Rotation))
                .AddTo(dependency.Component);
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

        public override void LateUpdate()
        {
            //指を適用する: FingerController経由じゃないことには注意
            if (_vmcpHandPose.IsActive.Value && _vmcpHandPose.IsConnected.Value)
            {
                _vmcpHandPose.ApplyFingerLocalPose();
            }
        }

        private readonly VMCPHandPose _vmcpHandPose;
        private readonly VMCPHandIkState _leftHandState;
        public override IHandIkState LeftHandState => _leftHandState;
        private readonly VMCPHandIkState _rightHandState;
        public override IHandIkState RightHandState => _rightHandState;
        public IReadOnlyReactiveProperty<bool> IsActive => _vmcpHandPose.IsActive;
        
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
            public bool SkipEnterIkBlend => true;
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