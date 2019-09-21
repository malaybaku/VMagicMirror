using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public class BodyMotionManagerReceiver : MonoBehaviour
    {
        [SerializeField] private ReceivedMessageHandler handler = null;

        [SerializeField] private BodyMotionManager bodyMotionManager = null;

        private float _scale = 1.0f;
        private Vector3 _waitingMotionSize;
        
        private void Start()
        {
            _waitingMotionSize = bodyMotionManager.WaitingBodyMotion.MotionSize;
            SetWaitMotionScale(1.25f);

            handler.Commands.Subscribe(message =>
            {
                switch (message.Command)
                {
                    case MessageCommandNames.EnableWaitMotion:
                        EnableWaitMotion(message.ToBoolean());
                        break;
                    case MessageCommandNames.WaitMotionScale:
                        SetWaitMotionScale(message.ParseAsPercentage());
                        break;
                    case MessageCommandNames.WaitMotionPeriod:
                        SetWaitMotionDuration(message.ToInt());
                        break;
                    default:
                        break;
                }
            });
        }

        public void EnableWaitMotion(bool isEnabled)
        {
            //isEnabled == falseでもビヘイビアは止めちゃダメ(動きかけのところで固定されてしまうので)
            bodyMotionManager.WaitingBodyMotion.MotionSize =
                isEnabled ? _scale * _waitingMotionSize : Vector3.zero;
        }

        public void SetWaitMotionScale(float scale)
        {
            _scale = scale;
            bodyMotionManager.WaitingBodyMotion.MotionSize = _scale * _waitingMotionSize;
        }

        public void SetWaitMotionDuration(float period)
            => bodyMotionManager.WaitingBodyMotion.Durations = new Vector3(period, period, period);
    }
}
