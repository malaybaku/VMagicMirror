﻿using UnityEngine;

namespace Baku.VMagicMirror
{
    public class BodyMotionManagerReceiver
    {
        public BodyMotionManagerReceiver(IMessageReceiver receiver, BodyMotionManager bodyMotionManager)
        {
            _bodyMotionManager = bodyMotionManager;

            receiver.AssignCommandHandler(
                VmmCommands.EnableWaitMotion,
                message => EnableWaitMotion(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                VmmCommands.WaitMotionScale,
                message => SetWaitMotionScale(message.ParseAsPercentage())
                );
            receiver.AssignCommandHandler(
                VmmCommands.WaitMotionPeriod,
                message => SetWaitMotionDuration(message.ToInt())
                );
            _waitingMotionSize = _bodyMotionManager.WaitingBodyMotion.MotionSize;
            SetWaitMotionScale(1.25f);
        }

        private readonly BodyMotionManager _bodyMotionManager;
        private readonly Vector3 _waitingMotionSize;
        
        private bool _isWaitMotionEnabled = true;
        private float _scale = 1.0f;
        
        private void EnableWaitMotion(bool isEnabled)
        {
            _isWaitMotionEnabled = isEnabled;
            
            //isEnabled == falseでもビヘイビアは止めちゃダメ(動きかけのところで固定されてしまうので)
            _bodyMotionManager.WaitingBodyMotion.MotionSize =
                _isWaitMotionEnabled ? _scale * _waitingMotionSize : Vector3.zero;
        }

        private void SetWaitMotionScale(float scale)
        {
            _scale = scale;
            _bodyMotionManager.WaitingBodyMotion.MotionSize = 
                _isWaitMotionEnabled ? _scale * _waitingMotionSize : Vector3.zero;
        }

        private void SetWaitMotionDuration(float period)
            => _bodyMotionManager.WaitingBodyMotion.Duration = period;
    }
}
