using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>指の制御をFingerControllerより後、かつマッスルベースで動かしたいときに使うやつ</summary>
    public class FingerLateController : MonoBehaviour
    {
        private const float DefaultBendingAngle = 10.0f;
        private const float HoldSpeedFactor = 18.0f;
        //角度ベースの値をMuscleのレートにおおよそ換算してくれるすごいやつだよ
        //観察ベースで「マッスル値をいくつ変えたら90度ぶん動くかな～」と調べてます
        private const float BendDegToMuscle = 1.65f / 90f;
        
        [Inject] private IVRMLoadable _vrmLoadable;
        
        private Animator _animator = null;
        //角度入力時に使うマッスル系の情報
        private HumanPoseHandler _humanPoseHandler = null;
        //Tポーズ時点のポーズ情報
        private HumanPose _defaultHumanPose = default;
        //毎フレーム書き換えるポーズ情報
        private HumanPose _humanPose = default;
        
        private readonly bool[] _hold = new bool[10];
        private readonly float[] _targetAngles = new float[10];
        //「指を曲げっぱなしにする/離す」というオペレーションによって決まる値
        private readonly float[] _holdOperationBendingAngle = new float[10];
        
        /// <summary>
        /// 特定の指の曲げ角度を固定します。
        /// </summary>
        /// <param name="fingerNumber"></param>
        /// <param name="angle"></param>
        public void Hold(int fingerNumber, float angle)
        {
            if (fingerNumber >= 0 && fingerNumber < _hold.Length)
            {
                _hold[fingerNumber] = true;
                _targetAngles[fingerNumber] = angle;
            }
        }

        /// <summary>
        /// <see cref="Hold"/>で固定していた指を解放します。
        /// </summary>
        /// <param name="fingerNumber"></param>
        public void Release(int fingerNumber)
        {
            if (fingerNumber >= 0 && fingerNumber < _hold.Length)
            {
                _hold[fingerNumber] = false;
                _targetAngles[fingerNumber] = 0;
            }
        }
        
        private void Start()
        {
            _vrmLoadable.VrmLoaded += info =>
            {
                _animator = info.animator;
                _humanPoseHandler = new HumanPoseHandler(_animator.avatar, _animator.transform);
                //とりあえず現在の値を拾っておく
                _humanPoseHandler.GetHumanPose(ref _humanPose);
                _defaultHumanPose = _humanPose;

                for (int i = 0; i < 10; i++)
                {
                    _hold[i] = false;
                    _targetAngles[i] = DefaultBendingAngle;
                }
            };
            
            _vrmLoadable.VrmDisposing += () =>
            {
                if (_animator == null)
                {
                    return;
                }
                _animator = null;
                _humanPoseHandler = null;
            };
        }

        private void LateUpdate()
        {
            if (_animator == null)
            {
                return;
            }
            
            _humanPoseHandler.GetHumanPose(ref _humanPose);

            for (int i = 0; i < 10; i++)
            {
                if (!_hold[i])
                {
                    continue;
                }
                
                _holdOperationBendingAngle[i] = Mathf.Lerp(
                    _holdOperationBendingAngle[i],
                    _targetAngles[i],
                    HoldSpeedFactor * Time.deltaTime
                    );
                
                float angle = _holdOperationBendingAngle[i];

                //MuscleをTポーズから変化させる値
                //常にマイナスの値を入れればOK: デフォルトから曲げ方向に動かすため
                float rate = -angle * BendDegToMuscle;
                FingerMuscleSetter.BendFinger(in _defaultHumanPose, ref _humanPose, i, rate);
            }
            _humanPoseHandler.SetHumanPose(ref _humanPose);
        }
    }
}
