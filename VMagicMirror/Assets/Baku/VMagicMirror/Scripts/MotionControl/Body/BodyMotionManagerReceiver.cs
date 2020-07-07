using UnityEngine;
using Zenject;
using Baku.VMagicMirror.InterProcess;

namespace Baku.VMagicMirror
{
    public class BodyMotionManagerReceiver : MonoBehaviour
    {
        //TODO: このマネージャコードにぶら下げて非MonoBehaviour化したい
        [SerializeField] private BodyMotionManager bodyMotionManager = null;

        private bool _isWaitMotionEnabled = true;
        private float _scale = 1.0f;
        private Vector3 _waitingMotionSize;

        [Inject]
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.AssignCommandHandler(
                MessageCommandNames.EnableWaitMotion,
                message => EnableWaitMotion(message.ToBoolean())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.WaitMotionScale,
                message => SetWaitMotionScale(message.ParseAsPercentage())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.WaitMotionPeriod,
                message => SetWaitMotionDuration(message.ToInt())
                );
            receiver.AssignCommandHandler(
                MessageCommandNames.EnableBodyLeanZ,
                message => bodyMotionManager.EnableImageBaseBodyLeanZ(message.ToBoolean())
                );
        }
        
        private void Start()
        {
            _waitingMotionSize = bodyMotionManager.WaitingBodyMotion.MotionSize;
            SetWaitMotionScale(1.25f);
        }

        private void EnableWaitMotion(bool isEnabled)
        {
            _isWaitMotionEnabled = isEnabled;
            
            //isEnabled == falseでもビヘイビアは止めちゃダメ(動きかけのところで固定されてしまうので)
            bodyMotionManager.WaitingBodyMotion.MotionSize =
                _isWaitMotionEnabled ? _scale * _waitingMotionSize : Vector3.zero;
        }

        private void SetWaitMotionScale(float scale)
        {
            _scale = scale;
            
            bodyMotionManager.WaitingBodyMotion.MotionSize = 
                _isWaitMotionEnabled ? _scale * _waitingMotionSize : Vector3.zero;
        }

        private void SetWaitMotionDuration(float period)
            => bodyMotionManager.WaitingBodyMotion.Duration = period;
    }
}
