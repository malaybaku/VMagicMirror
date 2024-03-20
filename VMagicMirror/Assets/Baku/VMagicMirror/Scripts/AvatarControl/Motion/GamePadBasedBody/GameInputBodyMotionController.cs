using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Baku.VMagicMirror.GameInput;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    public class GameInputBodyMotionController : PresenterBase, ITickable
    {
        private const float MoveLerpSmoothTime = 0.1f;
        private const float MoveLerpSmoothTimeOnThirdPerson = 0.05f;
        private const float LookAroundSmoothTime = 0.15f;

        private const float GunFireYawSmoothTime = 0.1f;
        private const float YawWhenGunFire = 45f;

        //スティックを右に倒したときに顔が右に向く量(deg)
        private const float HeadYawMaxDeg = 25f;
        //スティックを上下に倒したとき顔が上下に向く量(deg)
        private const float HeadPitchMaxDeg = 25f;

        //Hipsの並進が急だと違和感が出るので、ボーン回転だけシャープに補間させる
        private const float CustomMotionFadeInDuration = 0.05f;
        private const float CustomMotionFadeOutDuration = 0.25f;
        private const float CustomMotionHipFadeInDuration = 0.25f;
        private const float CustomMotionHipFadeOutDuration = 0.25f;
        
        private static readonly int Active = Animator.StringToHash("Active");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int Punch = Animator.StringToHash("Punch");
        private static readonly int GunFire = Animator.StringToHash("GunFire");
        private static readonly int MoveRight = Animator.StringToHash("MoveRight");
        private static readonly int MoveForward = Animator.StringToHash("MoveForward");
        private static readonly int Crouch = Animator.StringToHash("Crouch");
        private static readonly int Run = Animator.StringToHash("Run");
        private static readonly int Walk = Animator.StringToHash("Walk");

        private readonly IVRMLoadable _vrmLoadable;
        private readonly IMessageReceiver _receiver;
        private readonly BodyMotionModeController _bodyMotionModeController;
        private readonly GameInputBodyRootOrientationController _rootOrientationController;
        private readonly GameInputSourceSet _sourceSet;
        private readonly VrmaRepository _vrmaRepository;
        private readonly VrmaMotionSetter _vrmaMotionSetter;
        private readonly VrmaMotionSetterLocker _vrmaMotionSetterLocker = new();
        private readonly CancellationTokenSource _cts = new();

        private readonly HashSet<string> _customMotionActionKeys = new();
        private bool _customMotionActionKeysInitialized;
        
        private bool _hasModel;
        private Animator _animator;
        private int _baseLayerIndex;
        private Transform _vrmRoot;

        private GameInputLocomotionStyle _locomotionStyle = GameInputLocomotionStyle.FirstPerson;
        private bool _alwaysRun = true;
        private bool _bodyMotionActive;
        private bool _customMotionRunning;

        private Vector2 _rawMoveInput;
        private Vector2 _rawLookAroundInput;
        private bool _isCrouching;
        private bool _isRunWalkToggleActive;
        private bool _gunFire;
        private Vector2 _moveInput;
        private Vector2 _lookAroundInput;

        private Vector2 _moveInputDampSpeed;
        private Vector2 _lookAroundDampSpeed;
        
        private float _rootYaw;
        private float _rootYawDampSpeed;
        
        //NOTE: XORでも書けるが読み味がどっちもどっちなので…要は以下どっちかなら走るという事
        // - 基本歩き + 「ダッシュ」ボタンがオン
        // - 基本ダッシュ + 「歩く」ボタンがオフ
        private bool IsRunning => 
            (!_alwaysRun && _isRunWalkToggleActive) || 
            (_alwaysRun && !_isRunWalkToggleActive);
        
        public Quaternion LookAroundRotation { get; private set; } = Quaternion.identity;
        
        public GameInputBodyMotionController(
            IVRMLoadable vrmLoadable,
            IMessageReceiver receiver,
            BodyMotionModeController bodyMotionModeController,
            VrmaRepository vrmaRepository,
            VrmaMotionSetter vrmaMotionSetter,
            GameInputBodyRootOrientationController rootOrientationController,
            GameInputSourceSet sourceSet
            )
        {
            _vrmLoadable = vrmLoadable;
            _receiver = receiver;
            _bodyMotionModeController = bodyMotionModeController;
            _vrmaRepository = vrmaRepository;
            _vrmaMotionSetter = vrmaMotionSetter;
            _rootOrientationController = rootOrientationController;
            _sourceSet = sourceSet;
        }

        public override void Initialize()
        {
            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;

            _receiver.AssignCommandHandler(
                VmmCommands.EnableAlwaysRunGameInput,
                command => _alwaysRun = command.ToBoolean()
                );
            
            _receiver.AssignCommandHandler(
                VmmCommands.SetGameInputLocomotionStyle,
                command => SetLocomotionStyle(command.ToInt())
                );
            
            _bodyMotionModeController.MotionMode
                .Subscribe(mode => SetActiveness(mode == BodyMotionMode.GameInputLocomotion))
                .AddTo(this);
            
            Observable.Merge(_sourceSet.Sources.Select(s => s.Jump))
                .ThrottleFirst(TimeSpan.FromSeconds(0.2f))
                .Subscribe(_ => TryAct(Jump))
                .AddTo(this);
            Observable.Merge(_sourceSet.Sources.Select(s => s.Punch))
                .ThrottleFirst(TimeSpan.FromSeconds(0.2f))
                .Subscribe(_ => TryAct(Punch))
                .AddTo(this);

            Observable.Merge(_sourceSet.Sources.Select(s => s.StartCustomMotion))
                .ThrottleFirst(TimeSpan.FromSeconds(0.2f))
                .Subscribe(TryRunCustomMotion)
                .AddTo(this);

            //NOTE: 2デバイスから同時に来るのは許容したうえで、
            //ゲームパッド側はスティックが0付近のとき何も飛んでこない、というのを期待してる
            Observable.Merge(
                    _sourceSet.Sources.Select(s => s.MoveInput)
                )
                .Subscribe(v => _rawMoveInput = v)
                .AddTo(this);

            Observable.Merge(
                    _sourceSet.Sources.Select(s => s.LookAroundInput)
                )
                .Subscribe(v => _rawLookAroundInput = v)
                .AddTo(this);

            Observable.Merge(
                _sourceSet.Sources.Select(s => s.IsRunWalkToggleActive)
                )
                .Subscribe(v => _isRunWalkToggleActive = v)
                .AddTo(this);
            
            Observable.Merge(
                _sourceSet.Sources.Select(s => s.IsCrouching)
                )
                .Subscribe(v => _isCrouching = v)
                .AddTo(this);
            
            Observable.Merge(
                _sourceSet.Sources.Select(s => s.GunFire)
                )
                .Subscribe(v => _gunFire = v)
                .AddTo(this);            
        }

        public override void Dispose()
        {
            base.Dispose();
            _cts.Cancel();
            _cts.Dispose();
        }

        private void OnVrmLoaded(VrmLoadedInfo obj)
        {
            _animator = obj.animator;
            _vrmRoot = obj.vrmRoot;
            _baseLayerIndex = _animator.GetLayerIndex("Base");

            _hasModel = true;
            //モデルロードよりも先にゲーム入力が有効になってたときの適用漏れを防いでいる
            if (_bodyMotionActive)
            {
                _animator.SetBool(Active, true);
            }
        }

        private void OnVrmDisposing()
        {
            _hasModel = false;
            _animator = null;
            _vrmRoot = null;
            _rootOrientationController.ResetImmediately();
        }

        private void TryAct(int triggerHash)
        {
            if (!_hasModel || !_bodyMotionActive)
            {
                return;
            }

            var stateHash = _animator.GetCurrentAnimatorStateInfo(_baseLayerIndex).shortNameHash;
            
            //アクション中は無視する
            if ((stateHash == Walk || stateHash == Run || stateHash == Crouch) && 
                !_customMotionRunning
                )
            {
                _animator.SetTrigger(triggerHash);
            }
        }

        private void TryRunCustomMotion(string actionKey)
        {
            if (!_hasModel || !_bodyMotionActive || !IsAvailableCustomMotionKey(actionKey))
            {
                return;
            }

            var item = _vrmaRepository
                .GetAvailableFileItems()
                .First(i => i.FileName == actionKey);
            
            var stateHash = _animator.GetCurrentAnimatorStateInfo(_baseLayerIndex).shortNameHash;
            
            //要するにアクション中は無視する…というガード
            //CustomMotion中のCustomMotion呼び出しも許可しない
            if (!(stateHash == Walk || stateHash == Run || stateHash == Crouch) || 
                _customMotionRunning)
            {
                return;
            }

            if (!_vrmaMotionSetter.TryLock(_vrmaMotionSetterLocker))
            {
                return;
            }
            
            _customMotionRunning = true;
            RunCustomMotionAsync(item, _cts.Token).Forget();
        }

        private async UniTaskVoid RunCustomMotionAsync(VrmaFileItem item, CancellationToken ct)
        {
            _vrmaMotionSetter.FixHipLocalPosition = false;

            //やること: 適用率を0 > 1 > 0に遷移させつつ適用していく
            //prevのアニメーションを適用するかどうかは動的にチェックして決める
            _vrmaRepository.Run(item, false);
            var anim = _vrmaRepository.PeekInstance;
            var animDuration = _vrmaRepository.PeekInstance.Duration;
            var count = 0f;
            while (count < animDuration)
            {
                var rate = 1f;
                var hipRate = 1f;

                if (count > animDuration - CustomMotionHipFadeOutDuration)
                {
                    //終了間際, 1->0に下がっていく
                    _vrmaRepository.StopPrevAnimation();
                    hipRate = Mathf.Clamp01((animDuration - count) / CustomMotionHipFadeOutDuration);
                }
                else if (count < CustomMotionHipFadeInDuration)
                {
                    //0 -> 1, 始まってすぐ
                    hipRate = Mathf.Clamp01(count / CustomMotionHipFadeInDuration);
                }
                else
                {
                    // 中間部分。このタイミングで補間が要らなくなるので明示的に宣言しておく
                    _vrmaRepository.StopPrevAnimation();
                }                

                if (count > animDuration - CustomMotionFadeOutDuration)
                {
                    // 終了間近
                    rate = Mathf.Clamp01((animDuration - count) / CustomMotionFadeOutDuration);
                }
                else if (count < CustomMotionFadeInDuration)
                {
                    // 始まってすぐ
                    rate = Mathf.Clamp01(count / CustomMotionFadeInDuration);
                }
                
                //NOTE: rate == 1とかrate == 0のケースの最適化はmotionSetterにケアさせる
                if (_vrmaRepository.PrevInstance is { IsPlaying: true } playingPrev)
                {
                    //VRMAどうしの補間中にしか通らないパスで、通るのは珍しい
                    _vrmaMotionSetter.Set(playingPrev, anim, rate, hipRate);
                }
                else
                {
                    _vrmaMotionSetter.Set(anim, rate, hipRate);
                }
                
                //NOTE: LateTick相当くらいのタイミングを狙っていることに注意
                await UniTask.NextFrame(ct);
                count += Time.deltaTime;
            }            
            
            _vrmaRepository.StopCurrentAnimation();
            _vrmaMotionSetter.ReleaseLock();
            _customMotionRunning = false;
        }

        private void SetActiveness(bool active)
        {
            if (active == _bodyMotionActive)
            {
                return;
            }

            _bodyMotionActive = active;
            if (_hasModel)
            {
                _animator.SetBool(Active, _bodyMotionActive);
                if (!_bodyMotionActive)
                {
                    ResetParameters();
                    _rootOrientationController.ResetImmediately();
                }
            }
        }

        private void SetLocomotionStyle(int rawStyleValue)
        {
            if (rawStyleValue < (int)GameInputLocomotionStyle.FirstPerson ||
                rawStyleValue > (int)GameInputLocomotionStyle.SideView2D)
            {
                return;
            }

            var style = (GameInputLocomotionStyle)rawStyleValue;
            if (_locomotionStyle == style)
            {
                return;
            }

            _locomotionStyle = style;
            _rootOrientationController.LocomotionStyle = style;

            _moveInputDampSpeed = Vector2.zero;
        }

        private bool IsAvailableCustomMotionKey(string actionKey)
        {
            if (!_customMotionActionKeysInitialized)
            {
                _customMotionActionKeys.UnionWith(_vrmaRepository.GetAvailableMotionNames());
                _customMotionActionKeysInitialized = true;
            }
            return _customMotionActionKeys.Contains(actionKey);
        }
        
        private void ResetParameters()
        {
            _animator.SetFloat(MoveRight, 0f);
            _animator.SetFloat(MoveForward, 0f);
            _animator.SetBool(Crouch, false);
            _animator.SetBool(Run, false);
            _animator.SetBool(GunFire, false);

            _moveInput = Vector2.zero;
            _lookAroundInput = Vector2.zero;
            _moveInputDampSpeed = Vector2.zero;
            _lookAroundDampSpeed = Vector2.zero;
            _rootYaw = 0f;
            _rootYawDampSpeed = 0f;
            
            LookAroundRotation = Quaternion.identity;
        }
        
        //NOTE: イベント入力との順序とか踏まえて必要な場合、LateTickにしてもいい
        void ITickable.Tick()
        {
            //NOTE: モデルのロード中に非アクティブにした場合はパラメータがリセット済みのはず
            if (!_hasModel || !_bodyMotionActive)
            {
                return;
            }

            if (_locomotionStyle == GameInputLocomotionStyle.FirstPerson)
            {
                _moveInput = Vector2.SmoothDamp(
                    _moveInput, 
                    _rawMoveInput, 
                    ref _moveInputDampSpeed, 
                    MoveLerpSmoothTime
                );
            }
            else
            {
                _moveInput = Vector2.SmoothDamp(
                    _moveInput, 
                    _rawMoveInput, 
                    ref _moveInputDampSpeed, 
                    MoveLerpSmoothTimeOnThirdPerson
                );
            }

            _lookAroundInput = Vector2.SmoothDamp(
                _lookAroundInput, 
                _rawLookAroundInput, 
                ref _lookAroundDampSpeed,
                LookAroundSmoothTime
                );

            if (_locomotionStyle == GameInputLocomotionStyle.FirstPerson)
            {
                LookAroundRotation = Quaternion.Euler(
                    -_lookAroundInput.y * HeadPitchMaxDeg,
                    _lookAroundInput.x * HeadYawMaxDeg,
                    0f
                );
            }
            else
            {
                //NOTE: 3人称なりにヨー方向に目線を動かすのもアリで、やる場合は首だけよりはLookAtIKで上半身ごと動かしたい…
                LookAroundRotation = Quaternion.Euler(-_lookAroundInput.y * HeadPitchMaxDeg, 0f, 0f);
            }
            
            _rootOrientationController.UpdateInput(_moveInput, Time.deltaTime);

            switch (_locomotionStyle)
            {
                case GameInputLocomotionStyle.FirstPerson:
                    _animator.SetFloat(MoveRight, _moveInput.x);
                    _animator.SetFloat(MoveForward, _moveInput.y);
                    break;
                case GameInputLocomotionStyle.ThirdPerson:
                    //NOTE: 補間出来たほうが少しキレイではある。横スクロールも同様
                    _animator.SetFloat(MoveRight, 0f);
                    _animator.SetFloat(MoveForward, _moveInput.magnitude);
                    break;
                case GameInputLocomotionStyle.SideView2D:
                    _animator.SetFloat(MoveRight, 0f);
                    //1.2fを掛けるのは、左右ピッタリの入力ではなく多少斜めになっていた場合にも全速移動扱いするため
                    _animator.SetFloat(MoveForward, Mathf.Clamp01(Mathf.Abs(_moveInput.x) * 1.2f));
                    break;
            }

            _animator.SetBool(Crouch, _isCrouching);
            //NOTE: XORでも書けるが読み味がどっちもどっちなので…要は以下どっちかなら走るという事
            // - 基本歩き + 「ダッシュ」ボタンがオン
            // - 基本ダッシュ + 「歩く」ボタンがオフ
            _animator.SetBool(Run, IsRunning);
            _animator.SetBool(GunFire, _gunFire);

            _rootYaw = Mathf.SmoothDamp(
                _rootYaw, 
                _gunFire ? YawWhenGunFire : 0f, 
                ref _rootYawDampSpeed, 
                GunFireYawSmoothTime
                );
            _vrmRoot.localRotation = Quaternion.Euler(0f, _rootYaw, 0f) * _rootOrientationController.Rotation;
        }
    }
}
