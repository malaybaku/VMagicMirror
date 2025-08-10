using UnityEngine;
using Zenject;
using R3;

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

        private Animator _animator;
        //角度入力時に使うマッスル系の情報
        private HumanPoseHandler _humanPoseHandler;
        //Tポーズ時点のポーズ情報
        private HumanPose _defaultHumanPose;
        //毎フレーム書き換えるポーズ情報
        private HumanPose _humanPose;

        private readonly bool[] _hold = new bool[10];
        private readonly float[] _targetAngles = new float[10];
        //「指を曲げっぱなしにする/離す」というオペレーションによって決まる値
        private readonly float[] _holdOperationBendingAngle = new float[10];

        private bool _hasHoldCalledAtLeastOnce;
        private bool _isGameInputMode;
        
        [Inject]
        public void Initialize(IVRMLoadable vrmLoadable, BodyMotionModeController bodyMotionModeController)
        {
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += () =>
            {
                _animator = null;
                _humanPoseHandler = null;
            };

            bodyMotionModeController.MotionMode
                .Subscribe(mode => _isGameInputMode = mode == BodyMotionMode.GameInputLocomotion)
                .AddTo(this);
        }

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

            _hasHoldCalledAtLeastOnce = true;
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

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _animator = info.controlRig;
            _humanPoseHandler = new HumanPoseHandler(info.animator.avatar, info.animator.transform);
            //とりあえず現在の値を拾っておく
            _humanPoseHandler.GetHumanPose(ref _humanPose);
            _defaultHumanPose = _humanPose;

            for (int i = 0; i < 10; i++)
            {
                _hold[i] = false;
                _targetAngles[i] = DefaultBendingAngle;
            }
        }

        private void LateUpdate()
        {
            if (!_hasHoldCalledAtLeastOnce || _animator == null)
            {
                return;
            }

            //今から曲げるべき指があるとか、指の曲げ/戻し中であるような場合だけ処理する。
            //このガードにより、普段はMuscleのI/Oが走らないため、CPUにとてもやさしい
            bool needUpdate = false;
            for (int i = 0; i < _hold.Length; i++)
            {
                if (_hold[i])
                {
                    needUpdate = true;
                    break;
                }
            }

            if (!needUpdate)
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

            //ゲーム入力モードの場合は書き込みだけ禁止しておく = 値の遷移計算はそのまま回ってる状態という事にする
            if (!_isGameInputMode)
            {
                _humanPoseHandler.SetHumanPose(ref _humanPose);
            }
        }
    }
}
