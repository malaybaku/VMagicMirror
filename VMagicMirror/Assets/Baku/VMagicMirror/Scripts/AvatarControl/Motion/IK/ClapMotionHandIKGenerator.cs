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
        
        private readonly IKDataRecord _leftIk = new IKDataRecord();
        private readonly IKDataRecord _rightIk = new IKDataRecord ();
        private readonly ClapMotionHandIkState _leftState;
        private readonly ClapMotionHandIkState _rightState;
        public override IHandIkState LeftHandState => _leftState;
        public override IHandIkState RightHandState => _rightState;

        private readonly ClapMotionKeyPoseCalculator _keyPoseCalculator = new ClapMotionKeyPoseCalculator();
        private readonly ClapMotionTimeTableGenerator _timeTableGenerator = new ClapMotionTimeTableGenerator();
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
        private float _clapTime = 0f;

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

            //NOTE: 既に拍手中の場合はモーションのつなげ方に配慮する。
            // - 最初の拍手中 -> 連続で呼びすぎなので無視
            // - それ以降 -> 初動のモーションがやりすぎにならない点にだけ配慮して実行し、ほぼ繋がってるビジュアルにする
            var isAlwaysRunning = ClapMotionRunning;
            if (isAlwaysRunning)
            {
                var (phase, _) = _timeTableGenerator.GetPhaseAndRate(_clapTime);
                //最初の拍手中なので無視
                if (phase < ClapMotionPhase.MainClap)
                {
                    return;
                }
            }

            var motionScale = Random.Range(0.75f, 1f);
            _keyPoseCalculator.MotionScale = motionScale;
            _keyPoseCalculator.HandOffset = _avatarParamLoader.MeanOffset;

            //拍手前の手の位置からの軌道を求める
            var currentLeft = Dependency.HandIkGetter.GetLeft();
            var currentRight = Dependency.HandIkGetter.GetRight();
            _poseInterpolator.Refresh(
                new HandPoses(
                    new HandPose(currentLeft.Position, currentLeft.Rotation),
                    new HandPose(currentRight.Position, currentRight.Rotation)),
                isAlwaysRunning
                );

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
            //拍手動作は以下のステップに分かれるが、キーポーズと補間処理は他クラスに委託されてるので、基本それを呼ぶだけになっている
            //1. 両手を拍手の初期位置に持ってきて、極めて短い時間だけ静止する
            //2. 最初の「パチ」
            //3. それ以降の規則的な「パチパチ」
            //  - 最後の「パチ」モーションの戻しをゆっくりにしつつ、途中でステートを抜ける
            
            _clapTime = 0f;

            var stateQuited = false;
            
            //NOTE: SmoothDampしていいんでは、という考え方があるので現在値をキープしながら更新していく
            var prevPoses = startPoses;
            var poses = startPoses;
            
            while (_clapTime < _timeTableGenerator.TotalDuration)
            {
                var (phase, rate) = _timeTableGenerator.GetPhaseAndRate(_clapTime);

                switch (phase)
                {
                    case ClapMotionPhase.FirstEntry:
                        poses = _poseInterpolator.GetEntryPose(rate);
                        break;
                    case ClapMotionPhase.FirstClap:
                        poses = _poseInterpolator.GetFirstClap(rate);
                        break;
                    case ClapMotionPhase.MainClap:
                    case ClapMotionPhase.LastClap:
                        poses = _poseInterpolator.GetClap(rate);
                        break;
                    default:
                        //NOTE: 特にEndWaitでは直前状態を(ごく短い時間だけ)キープする
                        break;
                }

                //NOTE: フェーズによってはモーションを完全に適用せずに慣性をつけてもOK
                ApplyPoses(poses);
                prevPoses = poses;

                if (_clapTime > _timeTableGenerator.MotionStateDuration && !stateQuited)
                {
                    stateQuited = true;
                    //ステートを戻すが、動作はもう少し続く
                    ClapMotionRunning = false;
                    _leftState.RaisePrevStateRequest();
                    _rightState.RaisePrevStateRequest();

                    //NOTE: ヒジをいじるような実装が復活するならここでリセット処理もする
                    // if (_resetElbowOffsetCoroutine != null)
                    // {
                    //     StopCoroutine(_resetElbowOffsetCoroutine);
                    // }
                    // _resetElbowOffsetCoroutine = StartCoroutine(ResetElbowOffsets(...));
                }

                _clapTime += Time.deltaTime;
                yield return null;
            }
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
