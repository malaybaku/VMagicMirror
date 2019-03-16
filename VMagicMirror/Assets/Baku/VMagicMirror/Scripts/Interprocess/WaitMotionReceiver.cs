using UnityEngine;

namespace Baku.VMagicMirror
{
    public class WaitMotionReceiver : MonoBehaviour
    {
        [SerializeField]
        BodyTargetMove bodyTargetMove = null;

        private Vector3 _defaultSize;
        private float _scale;

        private void Start()
        {
            _scale = 1.0f;
            _defaultSize = bodyTargetMove.motionSize;
        }

        public void EnableWaitMotion(bool isEnabled)
        {
            if (isEnabled)
            {
                bodyTargetMove.motionSize = _scale * _defaultSize;
            }
            else
            {
                //単にdisableしてしまうと動きかけの状態でモデルが停止してしまう点に注意
                bodyTargetMove.motionSize = Vector3.zero;
            }
        }

        public void SetWaitMotionScale(float scale)
        {
            _scale = scale;
            bodyTargetMove.motionSize = _scale * _defaultSize;
        }

        //HACK: 値を二倍しているのはBodyTargetMove側が今のところ周期を半分にしてしまっているため
        public void SetWaitMotionDuration(float period)
            => bodyTargetMove.durations = new Vector3(period, period, period);
    }
}
