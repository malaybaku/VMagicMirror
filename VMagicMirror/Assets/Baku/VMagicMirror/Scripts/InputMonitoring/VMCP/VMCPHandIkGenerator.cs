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
        private readonly ReactiveProperty<bool> _isActive = new ReactiveProperty<bool>(false);
        public IReadOnlyReactiveProperty<bool> IsActive => _isActive;

        public void SetActive(bool active)
        {
            _isActive.Value = active;
            if (active)
            {
                _leftHandState.RaiseRequest();
                _rightHandState.RaiseRequest();
            }
        }

        //TODO: 送信側が直立ベースでIKを送って来る場合、ゲーム入力とかとの整合性を考えたい (ゲーム入力時は一括停止でもいいが)
        public void SetLeftHandPose(Vector3 position, Quaternion rotation)
        {
            _leftHandState.Position = position;
            _leftHandState.Rotation = rotation;
        }

        public void SetRightHandPose(Vector3 position, Quaternion rotation)
        {
            _rightHandState.Position = position;
            _rightHandState.Rotation = rotation;
        }

        public VMCPHandIkGenerator(HandIkGeneratorDependency dependency) : base(dependency)
        {
            _leftHandState = new VMCPHandIkState(ReactedHand.Left);
            _rightHandState = new VMCPHandIkState(ReactedHand.Right);
        }

        private readonly VMCPHandIkState _leftHandState;
        public override IHandIkState LeftHandState => _leftHandState;
        private readonly VMCPHandIkState _rightHandState;
        public override IHandIkState RightHandState => _rightHandState;

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