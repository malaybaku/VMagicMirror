using System;
using UnityEngine;
using Baku.VMagicMirror.IK;
using Zenject;

namespace Baku.VMagicMirror.MediaPipeTracker
{
    /// <summary>
    /// <see cref="BarracudaHandIK"/> の置き換えになるやつ
    /// - <see cref="HandIKIntegrator"/> では (Left|Right)Hand とかFingerの出力を参照する
    /// - MediaPipeTrackerの内部からは、このクラスにトラッキング結果を流し込む
    /// </summary>
    public class MediaPipeHand : ITickable
{
        [Inject]
        public MediaPipeHand()
        {
            _leftHandState = new MediaPipeHandState(ReactedHand.Left, _finger);
            _rightHandState = new MediaPipeHandState(ReactedHand.Right, _finger);
        }

        private readonly MediaPipeHandFinger _finger = new();

        private readonly MediaPipeHandState _leftHandState;
        public IHandIkState LeftHandState => _leftHandState;

        private readonly MediaPipeHandState _rightHandState;
        public IHandIkState RightHandState => _rightHandState;

        private HandIkGeneratorDependency _dependency;
        private bool IsInitialized => _dependency != null;

        public void SetDependency(HandIkGeneratorDependency dependency)
        {
            _dependency = dependency;
        }
        
        // NOTE: HandIkIntegratorにUpdate/LateUpdateを呼ばせるスタイルにしてもよいかも。タイミングの都合次第になる
        void ITickable.Tick()
        {
            if (!IsInitialized)
            {
                return;
            }
            
        }

        private class MediaPipeHandFinger
        {
            public void ReleaseLeftHand()
            {
                throw new NotImplementedException();
            }

            public void ReleaseRightHand()
            {
                throw new NotImplementedException();
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