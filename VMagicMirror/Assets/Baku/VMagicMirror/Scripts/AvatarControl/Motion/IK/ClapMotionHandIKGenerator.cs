using System;
using System.Collections;
using Baku.VMagicMirror.IK.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Baku.VMagicMirror.IK
{
    public class ClapMotionHandIKGenerator : HandIkGeneratorBase
    {
        private const int ClapCountMin = 6;
        private const int ClapCountMax = 12;
        
        private readonly IKDataRecord _leftIk = new();
        private readonly IKDataRecord _rightIk = new();
        private readonly ClapMotionHandIkState _leftState;
        private readonly ClapMotionHandIkState _rightState;
        public override IHandIkState LeftHandState => _leftState;
        public override IHandIkState RightHandState => _rightState;

        private readonly ClapMotionKeyPoseCalculator _keyPoseCalculator = new();
        private readonly ClapMotionTimeTableGenerator _timeTableGenerator = new();
        private readonly ClapMotionPoseInterpolator _poseInterpolator;
        private readonly ClapMotionElbowController _elbowController;
        private readonly ClapFingerController _fingerController;

        private readonly ElbowMotionModifier _elbowMotionModifier;
        private readonly ColliderBasedAvatarParamLoader _avatarParamLoader;
        
        private bool _hasModel;
        private Coroutine _resetElbowOffsetCoroutine;
        private Coroutine _clapCoroutine;

        public bool ClapMotionRunning { get; private set; }
        public float MotionDuration => _timeTableGenerator.TotalDuration;

        public ClapMotionHandIKGenerator(
            HandIkGeneratorDependency dependency, 
            IVRMLoadable vrmLoadable,
            ElbowMotionModifier elbowMotionModifier,
            ColliderBasedAvatarParamLoader avatarParamLoader
            ) : base(dependency)
        {
            _elbowMotionModifier = elbowMotionModifier;
            _avatarParamLoader = avatarParamLoader;

            _poseInterpolator = new ClapMotionPoseInterpolator(_keyPoseCalculator);
            _fingerController = new ClapFingerController(Dependency.Reactions.FingerController);
            _elbowController = new ClapMotionElbowController(_elbowMotionModifier, Dependency.Component);
            
            _leftState = new ClapMotionHandIkState(this, ReactedHand.Left);
            _rightState = new ClapMotionHandIkState(this, ReactedHand.Right);
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposed;
        }

        
        public void RunClapMotion()
        {
            if (!_hasModel)
            {
                return;
            }

            var motionScale = Random.Range(0.75f, 1f);
            _keyPoseCalculator.MotionScale = motionScale;
            _keyPoseCalculator.HandOffset = _avatarParamLoader.MeanOffset;

            //拍手前の手の位置からの軌道を求める
            var currentLeft = Dependency.HandIkGetter.GetLeft();
            var currentRight = Dependency.HandIkGetter.GetRight();
            _poseInterpolator.Refresh(new HandPoses(
                new HandPose(currentLeft.Position, currentLeft.Rotation),
                new HandPose(currentRight.Position, currentRight.Rotation)
                ));

            var startPoses = _poseInterpolator.StartPoses;

            _timeTableGenerator.Calculate(
                startPoses,
                _poseInterpolator.FirstDistantPoses,
                Random.Range(ClapCountMin, ClapCountMax),
                motionScale
                );

            ApplyPoses(startPoses);
            
            ClapMotionRunning = true;
            _leftState.RaiseRequest();
            _rightState.RaiseRequest();

            if (_resetElbowOffsetCoroutine != null)
            {
                StopCoroutine(_resetElbowOffsetCoroutine);
            }
            
            if (_clapCoroutine != null)
            {
                StopCoroutine(_clapCoroutine);
            }
            _clapCoroutine = StartCoroutine(ClapCoroutine(startPoses));            
        }

        public void StopClapMotion()
        {
            if (!ClapMotionRunning)
            {
                return;
            }
            
            //TODO: コルーチンを止めて直前ステートに抜ける
        }

        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            _keyPoseCalculator.SetupAvatarBodyParameter(info.animator);
            var headHeight = info.animator.GetBoneTransform(HumanBodyBones.Head).position.y;
            //0を入れると0除算になるので一応避けておく
            _timeTableGenerator.HeadHeight = Mathf.Max(headHeight, 0.1f);
            _hasModel = true;
        }

        private void OnVrmDisposed()
        {
            _hasModel = false;
        }

        private void OnEnterState()
        {
            _fingerController.Enable();
            _elbowController.ApplyElbowModify(_timeTableGenerator.FirstEntryDuration);
        }

        private void OnQuitState()
        {
            _fingerController.Release();
            //この時間は割と適当でもよいが、HandIkIntegrator.HandIkToggleDurationAfterClapに近い値だと無難
            _elbowController.ResetElbowModify(0.6f);
        }

        private IEnumerator ClapCoroutine(HandPoses startPoses)
        {
            //拍手動作は以下のステップに分かれるが、4以外はキーポーズと補間が他クラスに委託されてるので、それを呼ぶだけ
            //1. 両手を拍手の初期位置に持ってきて、極めて短い時間だけ静止する
            //2. 最初の「パチ」
            //3. それ以降の規則的な「パチパチ」
            //4. 最後の「パチ」の後に指を気持ちだけ制御しつつ、直前のステートに戻す


            //1, 2, 3
            var time = 0f;
            
            //NOTE: SmoothDampしていいんでは、という考え方があるので現在値をキープしながら更新していく
            var prevPoses = startPoses;
            var poses = startPoses;
            while (time < _timeTableGenerator.TotalDuration)
            {
                var (phase, rate) = _timeTableGenerator.GetPhaseAndRate(time);

                switch (phase)
                {
                    case ClapMotionPhase.FirstEntry:
                        poses = _poseInterpolator.GetEntryPose(rate);
                        break;
                    case ClapMotionPhase.FirstClap:
                        poses = _poseInterpolator.GetFirstClap(rate);
                        break;
                    case ClapMotionPhase.MainClap:
                        poses = _poseInterpolator.GetClap(rate);
                        break;
                    case ClapMotionPhase.EndWait:
                    default:
                        //NOTE: 特にEndWaitでは直前状態を(ごく短い時間だけ)キープする
                        break;
                }

                //NOTE: フェーズによってはモーションを完全に適用せずに慣性をつけてもOK
                ApplyPoses(poses);
                prevPoses = poses;

                time += Time.deltaTime;
                yield return null;
            }

            //4. ステート戻し
            ClapMotionRunning = false;
            _leftState.RaisePrevStateRequest();
            _rightState.RaisePrevStateRequest();

            //NOTE: ヒジを何かいじる実装に変えた場合、ここをちゃんと呼ぶ
            // if (_resetElbowOffsetCoroutine != null)
            // {
            //     StopCoroutine(_resetElbowOffsetCoroutine);
            // }
            // _resetElbowOffsetCoroutine = StartCoroutine(ResetElbowOffsets(
            //     0f, 0f, 0.5f
            // ));
        }
        
        // 拍手が終わったあとにヒジの広げ方を元に戻すやつ
        private IEnumerator ResetElbowOffsets(float leftElbowOffset, float rightElbowOffset, float duration)
        {
            //NOTE: ヒジをいじる実装が復活したら復活させます
            yield break;
            
            var time = 0f;
            while (time < duration)
            {
                var rate = Mathf.SmoothStep(1, 0, time / duration);
                _elbowMotionModifier.LeftElbowPositionOffset = Vector3.left * (rate * leftElbowOffset);
                _elbowMotionModifier.RightElbowPositionOffset = Vector3.right * (rate * rightElbowOffset);
                time += Time.deltaTime;
                yield return null;
            }
        }

        private void ApplyPoses(HandPoses poses)
        {
            _leftIk.Position = poses.Left.Position;
            _leftIk.Rotation = poses.Left.Rotation;
            _rightIk.Position = poses.Right.Position;
            _rightIk.Rotation = poses.Right.Rotation;
        }
        
        //NOTE: private classだし…ということで相互参照を許容している
        private class ClapMotionHandIkState : IHandIkState
        {
            public ClapMotionHandIkState(ClapMotionHandIKGenerator parent, ReactedHand hand)
            {
                _parent = parent;
                Hand = hand;
                _data = Hand == ReactedHand.Right ? _parent._rightIk : _parent._leftIk;
            }

            private readonly ClapMotionHandIKGenerator _parent;
            private readonly IIKData _data;
            public IHandIkState PrevState { get; private set; }

            //ブレンド処理をスキップさせ、自力でぜんぶ軌道生成する
            public bool SkipEnterIkBlend => true;

            public Vector3 Position => _data.Position;
            public Quaternion Rotation => _data.Rotation;
            public ReactedHand Hand { get; }
            public HandTargetType TargetType => HandTargetType.ClapMotion;

            public event Action<IHandIkState> RequestToUse;
            
            public void RaiseRequest() => RequestToUse?.Invoke(this);

            public void RaisePrevStateRequest()
            {
                if (PrevState != null)
                {
                    RequestToUse?.Invoke(PrevState);
                }
            }

            public void Enter(IHandIkState prevState)
            {
                //拍手し終えたときの遷移先はデフォルトでは遷移元である、ということ
                if (prevState != this)
                {
                    PrevState = prevState;
                }

                //NOTE: 二重呼び出しの防止のため、右手からだけ通知
                if (Hand == ReactedHand.Right)
                {
                    _parent.OnEnterState();
                }
            }

            public void Quit(IHandIkState nextState)
            {
                //NOTE: 二重呼び出しの防止のため、右手からだけ通知
                if (Hand == ReactedHand.Right)
                {
                    _parent.OnQuitState();
                }
            }
        }
    }
}
