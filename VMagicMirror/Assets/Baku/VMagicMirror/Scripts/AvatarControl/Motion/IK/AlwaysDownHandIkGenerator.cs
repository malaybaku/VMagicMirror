using System;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    /// <summary>常に手を下げた姿勢になるような手IKの生成処理。</summary>
    public sealed class AlwaysDownHandIkGenerator : HandIkGeneratorBase
    {
        private readonly SwitchableHandDownIkData _handDownIk;
        public IIKData LeftHand => _handDownIk.LeftHand;
        public IIKData RightHand => _handDownIk.RightHand;

        private readonly AlwaysHandDownState _leftHandState;
        public override IHandIkState LeftHandState => _leftHandState;

        private readonly AlwaysHandDownState _rightHandState;
        public override IHandIkState RightHandState => _rightHandState; 
        
        public AlwaysDownHandIkGenerator(
            HandIkGeneratorDependency dependency, SwitchableHandDownIkData switchableHandDownIk)
            : base(dependency)
        {
            _handDownIk = switchableHandDownIk;
            _leftHandState = new AlwaysHandDownState(this, ReactedHand.Left);
            _rightHandState = new AlwaysHandDownState(this, ReactedHand.Right);

            dependency.Config.IsAlwaysHandDown
                .Subscribe(v =>
                {
                    if (v)
                    {
                        _leftHandState.RaiseRequest();
                        _rightHandState.RaiseRequest();
                    }
                })
                .AddTo(dependency.Component);
        }

        private class AlwaysHandDownState : IHandIkState
        {
            public AlwaysHandDownState(AlwaysDownHandIkGenerator parent, ReactedHand hand)
            {
                Hand = hand;
                _data = hand == ReactedHand.Right ? parent.RightHand : parent.LeftHand;
            }

            private readonly IIKData _data;

            public bool SkipEnterIkBlend => false;
            public void RaiseRequest() => RequestToUse?.Invoke(this);

            public Vector3 Position => _data.Position;
            public Quaternion Rotation => _data.Rotation;
            public ReactedHand Hand { get; }
            public HandTargetType TargetType => HandTargetType.AlwaysDown;
            
            public event Action<IHandIkState> RequestToUse;

            public void Enter(IHandIkState prevState)
            {
            }

            public void Quit(IHandIkState nextState)
            {
            }
        }
    }
}
