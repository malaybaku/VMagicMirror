using System;
using Baku.VMagicMirror.MotionExporter;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.WordToMotion
{
    public class CustomMotionPlayRoutine : IDisposable
    {
        public CustomMotionPlayRoutine(
            HumanPoseHandler humanPoseHandler,
            HumanoidAnimationSetter setterFront,
            HumanoidAnimationSetter setterBack,
            IObservable<Unit> lateUpdateSource)
        {
            _currentState = new CustomMotionPlayState(humanPoseHandler, setterFront, lateUpdateSource);
            _prevState = new CustomMotionPlayState(humanPoseHandler, setterBack, lateUpdateSource);
        }

        private CustomMotionPlayState _currentState;
        private CustomMotionPlayState _prevState;

        public CustomMotionItem CurrentItem => _currentState.CurrentItem;
        public bool IsRunningLoopMotion => _currentState.IsRunningLoopMotion;
        public bool HasUpdate => _currentState.HasUpdate || _prevState.HasUpdate;

        public void ResetUpdateFlag()
        {
            _currentState.ResetUpdateFlag();
            _prevState.ResetUpdateFlag();
        }
        
        public void Run(CustomMotionItem item)
        {
            Swap(); 
            _currentState.RunMotion(item);
            _prevState.FadeOutCurrentMotion();
        }

        public void RunLoop(CustomMotionItem item)
        {
            Swap();
            _currentState.RunLoopMotion(item);
            _prevState.FadeOutCurrentMotion();
        }

        public void Stop()
        {
            Swap();
            _prevState.FadeOutCurrentMotion();
        }
        
        public void StopImmediate()
        {
            _currentState.StopImmediate();
            _prevState.StopImmediate();
        }

        public void Dispose()
        {
            _currentState.Dispose();
            _prevState.Dispose();
        }
        
        private void Swap() 
            => (_currentState, _prevState) = (_prevState, _currentState);
    }
}
