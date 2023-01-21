using Baku.VMagicMirror.IK;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class SwitchableHandDownIkData
    {
        private readonly SwitchIKData _leftHand;
        public IIKData LeftHand => _leftHand;
        private readonly SwitchIKData _rightHand;
        public IIKData RightHand => _rightHand; 

        public SwitchableHandDownIkData(
            CustomizedDownHandIk customizedDownHandIk,
            HandDownIkCalculator handDownIkCalculator)
        {
            _leftHand = new SwitchIKData(
                customizedDownHandIk.LeftHand, 
                handDownIkCalculator.LeftHand,
                customizedDownHandIk.EnableCustomHandDownPose
            );

            _rightHand = new SwitchIKData(
                customizedDownHandIk.RightHand, 
                handDownIkCalculator.RightHand,
                customizedDownHandIk.EnableCustomHandDownPose
            );
        }

        class SwitchIKData : IIKData
        {
            private readonly IIKData _x;
            private readonly IIKData _y;
            //NOTE: Funcを持たすほどでもないので手抜きしてます
            private readonly IReadOnlyReactiveProperty<bool> _useX;

            public SwitchIKData(IIKData x, IIKData y, IReadOnlyReactiveProperty<bool> useX)
            {
                _x = x;
                _y = y;
                _useX = useX;
            }

            public Vector3 Position => _useX.Value ? _x.Position : _y.Position;
            public Quaternion Rotation => _useX.Value ? _x.Rotation : _y.Rotation;
        }
    }
}
