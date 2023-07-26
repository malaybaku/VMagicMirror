using System;
using Baku.VMagicMirror.IK;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.VMCP
{
    //TODO: VMCPHandPoseに依存する形でIKの姿勢を言わせるようにする
    //NOTE: 上記の実現方法としてはコンストラクタ直すかDependencyにVMCPHandPoseを含めればよい
    public class VMCPHandIkGenerator : HandIkGeneratorBase
    {
        public VMCPHandIkGenerator(HandIkGeneratorDependency dependency, VMCPHandPose vmcpHandPose) : base(dependency)
        {
            _vmcpHandPose = vmcpHandPose;
            _leftHandState = new VMCPHandIkState(ReactedHand.Left);
            _rightHandState = new VMCPHandIkState(ReactedHand.Right);

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
                .Subscribe(pose =>
                {
                    _leftHandState.Position = pose.Position;
                    _leftHandState.Rotation = pose.Rotation;
                })
                .AddTo(dependency.Component);
            
            vmcpHandPose.RightHandPose
                .Subscribe(pose =>
                {
                    _rightHandState.Position = pose.Position;
                    _rightHandState.Rotation = pose.Rotation;
                })
                .AddTo(dependency.Component);
        }

        public override void LateUpdate()
        {
            //指を適用する: FingerController経由じゃないことには注意
            _vmcpHandPose.ApplyFingerLocalPose();
        }

        private readonly VMCPHandPose _vmcpHandPose;
        private readonly VMCPHandIkState _leftHandState;
        public override IHandIkState LeftHandState => _leftHandState;
        private readonly VMCPHandIkState _rightHandState;
        public override IHandIkState RightHandState => _rightHandState;
        public IReadOnlyReactiveProperty<bool> IsActive => _vmcpHandPose.IsActive;

        class VMCPHandIkState : IHandIkState
        {
            public VMCPHandIkState(ReactedHand hand)
            {
                Hand = hand;
            }
            
            public Vector3 Position { get; set; }
            public Quaternion Rotation { get; set; } = Quaternion.identity;
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