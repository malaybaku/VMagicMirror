using Baku.VMagicMirror.IK;
using Baku.VMagicMirror.MediaPipeTracker;
using Baku.VMagicMirror.VMCP;
using R3;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// ユーザーの入力と設定に基づいて、実際にIKを適用していくやつ
    /// </summary>
    public class HandIKIntegrator : MonoBehaviour, IHandIkGetter
    {
        #region settings
        
        //NOTE: ステートパターンがめんどくさいときのステートマシンの実装です。まあステート数少ないので…

        /// <summary> IK種類が変わるときのブレンディングに使う時間。IK自体の無効化/有効化もこの時間で行う </summary>
        private const float HandIkToggleDuration = 0.25f;
        //拍手からそれ以外のステートに移った場合にのみ用いるブレンディング時間。あまり急だと違和感あるので、ゆっくり腕を降ろさせるのが狙い
        private const float HandIkToggleDurationAfterClap = 0.6f;
        private static readonly float MaxHandIkToggleDuration =
            Mathf.Max(HandIkToggleDuration, HandIkToggleDurationAfterClap);

        private const float HandIkTypeChangeCoolDown = 0.3f;

        //この時間だけ入力がなかったらマウスやキーボードの操作をしている手を下ろしてもいいよ、という秒数
        //たぶん無いと思うけど、何かの周期とピッタリ合うと嫌なのでてきとーに小数値を載せてます
        public const float AutoHandDownDuration = 10.5f;

        //NOTE: 2秒かけて下ろし、0.4秒で戻す、という速度。戻すほうがスピーディなことに注意
        public const float HandDownBlendSpeed = 1f / 2f;
        public const float HandUpBlendSpeed = 1f / 0.4f;

        [SerializeField] private TypingHandIKGenerator typing = null;
        public TypingHandIKGenerator Typing => typing;

        [SerializeField] private GamepadFingerController gamepadFinger = null;
        [SerializeField] private ArcadeStickFingerController arcadeStickFinger = null;
        [SerializeField] private WaitingBodyMotion waitingBody = null;
        [SerializeField] private FingerController fingerController = null;
        [SerializeField] private GamepadHandIKGenerator.GamepadHandIkGeneratorSetting gamepadSetting = default;

        //TODO: 相互参照になっててキモいのでできれば直してほしい…
        [SerializeField] private ElbowMotionModifier elbowMotionModifier;
        
        public MouseMoveHandIKGenerator MouseMove { get; private set; }
        public GamepadHandIKGenerator GamepadHand { get; private set; }
        public MidiHandIkGenerator MidiHand { get; private set; }
        public PresentationHandIKGenerator Presentation { get; private set; }
        public CarHandleIkGenerator CarHandle { get; private set; }
        public ArcadeStickHandIKGenerator ArcadeStickHand { get; private set; }
        public PenTabletHandIKGenerator PenTabletHand { get; private set; }

        private AlwaysDownHandIkGenerator _downHand;
        public ClapMotionHandIKGenerator ClapMotion { get; private set; }
        private MediaPipeHand _mediaPipeHand;
        
        private Transform _rightHandTarget = null;
        private Transform _leftHandTarget = null;

        private float _leftHandStateBlendCount = 0f;
        private float _rightHandStateBlendCount = 0f;

        private float _leftHandIkChangeCoolDown = 0f;
        private float _rightHandIkChangeCoolDown = 0f;

        private bool _enableHidArmMotion = true;

        public bool EnableHidArmMotion
        {
            get => _enableHidArmMotion;
            set
            {
                _enableHidArmMotion = value;
                MouseMove.EnableUpdate = value;
                PenTabletHand.EnableUpdate = value;
            }
        }

        private bool _enableHandDownTimeout = true;

        public bool EnableHandDownTimeout
        {
            get => _enableHandDownTimeout;
            set
            {
                _enableHandDownTimeout = value;
                typing.EnableHandDownTimeout = value;
                MouseMove.EnableHandDownTimeout = value;
            }
        }

        public ReactiveProperty<WordToMotionDeviceAssign> WordToMotionDevice { get; } =
            new(WordToMotionDeviceAssign.KeyboardWord);
        
        public bool EnablePresentationMode => _keyboardAndMouseMotionMode.Value == KeyboardAndMouseMotionModes.Presentation;
        
        public void SetKeyboardAndMouseMotionMode(int modeIndex)
        {
            if (modeIndex >= -1 &&
                modeIndex < (int) KeyboardAndMouseMotionModes.Unknown &&
                modeIndex != (int) _keyboardAndMouseMotionMode.Value
                )
            {
                var mode = (KeyboardAndMouseMotionModes) modeIndex;
                //NOTE: オプションを変えた直後は手は動かさず、変更後の入力によって手が動く
                _keyboardAndMouseMotionMode.Value = mode;
                //Noneは歴史的経緯によって扱いが特殊なので注意
                EnableHidArmMotion = mode != KeyboardAndMouseMotionModes.None;
            }
        }

        public void SetGamepadMotionMode(int modeIndex)
        {
            if (modeIndex >= 0 &&
                modeIndex < (int) GamepadMotionModes.Unknown &&
                modeIndex != (int) _gamepadMotionMode.Value
                )
            {   
                _gamepadMotionMode.Value = (GamepadMotionModes) modeIndex;
                //NOTE: オプションを変えた直後は手はとりあえず動かさないでおく(変更後の入力によって手が動く)。
                //変えた時点で切り替わるほうが嬉しい、と思ったらそういう挙動に直す
            }
        }

        private readonly ReactiveProperty<GamepadMotionModes> _gamepadMotionMode = new(GamepadMotionModes.Gamepad);

        private readonly ReactiveProperty<KeyboardAndMouseMotionModes> _keyboardAndMouseMotionMode = 
            new(KeyboardAndMouseMotionModes.KeyboardAndTouchPad);
        
        //NOTE: これはすごく特別なフラグで、これが立ってると手のIKに何か入った場合でも手が下がりっぱなしになる
        public ReactiveProperty<bool> AlwaysHandDown { get; } = new(false);

        public bool IsLeftHandGripGamepad => _leftTargetType.Value == HandTargetType.Gamepad;
        public bool IsRightHandGripGamepad => _rightTargetType.Value == HandTargetType.Gamepad;
        
        public Vector3 RightHandPosition => _rightHandTarget.position;
        public Vector3 LeftHandPosition => _leftHandTarget.position;

        public float YOffsetAlways
        {
            get => Typing.YOffsetAlways;
            set
            {
                Typing.YOffsetAlways = value;
                MouseMove.YOffset = value;
                PenTabletHand.YOffset = value;
                MidiHand.HandOffsetAlways = value;
            }
        }

        #endregion

        private HandIkReactionSources _reactionSources;
        private readonly HandIkInputEvents _inputEvents = new HandIkInputEvents();
        
        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            IKTargetTransforms ikTargets, 
            Camera cam,
            ParticleStore particleStore,
            KeyboardProvider keyboardProvider,
            GamepadProvider gamepadProvider,
            MidiControllerProvider midiControllerProvider,
            TouchPadProvider touchPadProvider,
            ArcadeStickProvider arcadeStickProvider,
            CarHandleAngleGenerator carHandleAngleGenerator,
            CarHandleProvider carHandleProvider,
            CarHandleFingerController carHandleFingerController,
            PenTabletProvider penTabletProvider,
            ColliderBasedAvatarParamLoader colliderBasedAvatarParamLoader,
            SwitchableHandDownIkData switchableHandDownIk,
            VMCPHandPose vmcpHandPose,
            VMCPFingerController vmcpFingerController,
            MediaPipeHand mediaPipeHand
            )
        {
            _reactionSources = new HandIkReactionSources(
                particleStore,
                fingerController,
                gamepadFinger,
                arcadeStickFinger
            );
            var runtimeConfig = new HandIkRuntimeConfigs(
                AlwaysHandDown,
                _leftTargetType,
                _rightTargetType,
                _keyboardAndMouseMotionMode,
                _gamepadMotionMode,
                WordToMotionDevice,
                CheckCoolDown,
                CheckTypingOrMouseHandsCanMoveDown
            );
            var dependency = new HandIkGeneratorDependency(
                this, _reactionSources, runtimeConfig, _inputEvents, this
                );

            _rightHandTarget = ikTargets.RightHand;
            _leftHandTarget = ikTargets.LeftHand;

            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;        

            MouseMove = new MouseMoveHandIKGenerator(dependency, touchPadProvider);
            MidiHand = new MidiHandIkGenerator(dependency, midiControllerProvider);
            GamepadHand = new GamepadHandIKGenerator(
                dependency, vrmLoadable, waitingBody, gamepadProvider, gamepadSetting
                );
            Presentation = new PresentationHandIKGenerator(dependency, vrmLoadable, cam);
            ArcadeStickHand = new ArcadeStickHandIKGenerator(dependency, vrmLoadable, arcadeStickProvider);
            CarHandle = new CarHandleIkGenerator(dependency, carHandleAngleGenerator, carHandleProvider, carHandleFingerController);
            _downHand = new AlwaysDownHandIkGenerator(dependency, switchableHandDownIk);
            PenTabletHand = new PenTabletHandIKGenerator(dependency, vrmLoadable, penTabletProvider);
            ClapMotion = new ClapMotionHandIKGenerator(dependency, vrmLoadable, elbowMotionModifier, colliderBasedAvatarParamLoader);

            _mediaPipeHand = mediaPipeHand;
            _mediaPipeHand.SetDependency(dependency, _downHand);

            typing.SetUp(keyboardProvider, dependency);

            MouseMove.DownHand = _downHand;
            typing.DownHand = _downHand;

            //TODO: TypingだけMonoBehaviourなせいで若干ダサい
            foreach (var generator in new HandIkGeneratorBase[]
                {
                    MouseMove, MidiHand, GamepadHand, ArcadeStickHand, CarHandle, Presentation, _downHand, PenTabletHand, ClapMotion,
                })
            {
                if (generator.LeftHandState != null)
                {
                    generator.LeftHandState.RequestToUse += SetLeftHandState;
                }

                if (generator.RightHandState != null)
                {
                    generator.RightHandState.RequestToUse += SetRightHandState;
                }
            }
            
            Typing.LeftHand.RequestToUse += SetLeftHandState;
            Typing.RightHand.RequestToUse += SetRightHandState;
            _mediaPipeHand.LeftHandState.RequestToUse += SetLeftHandState;
            _mediaPipeHand.RightHandState.RequestToUse += SetRightHandState;
        }

        //NOTE: prevのStateは初めて手がキーボードから離れるまではnull
        private IHandIkState _prevRightHand = null;
        private IHandIkState _prevLeftHand = null;

        //NOTE: こっちはStart()以降は非null
        private IHandIkState _currentRightHand = null;
        private IHandIkState _currentLeftHand = null;

        //NOTE: 値自体はCurrentRightHand.TargetTypeとかと等しい。値を他のIKに露出するために使う
        private readonly ReactiveProperty<HandTargetType> _leftTargetType = new(HandTargetType.Keyboard);
        private readonly ReactiveProperty<HandTargetType> _rightTargetType = new(HandTargetType.Keyboard);
        public ReadOnlyReactiveProperty<HandTargetType> LeftTargetType => _leftTargetType;
        public ReadOnlyReactiveProperty<HandTargetType> RightTargetType => _rightTargetType;

        #region API

        #region Keyboard and Mouse
        
        public void KeyDown(string keyName)
        {
            if (!EnableHidArmMotion)
            {
                return;
            }
            _inputEvents.RaiseKeyDown(keyName);
        }

        public void KeyUp(string keyName)
        {
            if (!EnableHidArmMotion)
            {
                return;
            }
            _inputEvents.RaiseKeyUp(keyName);
        }
        

        public void MoveMouse(Vector3 mousePosition)
        {
            var targetType =
                (_keyboardAndMouseMotionMode.Value == KeyboardAndMouseMotionModes.KeyboardAndTouchPad) ? HandTargetType.Mouse :
                (_keyboardAndMouseMotionMode.Value == KeyboardAndMouseMotionModes.Presentation) ? HandTargetType.Presentation :
                    HandTargetType.PenTablet;

            if (!EnableHidArmMotion || !CheckCoolDown(ReactedHand.Right, targetType))
            {
                return;
            }
            
            _inputEvents.RaiseMoveMouse(mousePosition);
        }

        public void OnMouseButton(string button)
        {
            if (!EnablePresentationMode && EnableHidArmMotion && !AlwaysHandDown.Value)
            {
                _inputEvents.RaiseOnMouseButton(button);
            }
        }

        #endregion
        
        #region Gamepad
        
        //NOTE: ButtonDown/ButtonUpで反応する手が非自明なものはHandIk側でCooldownチェックをしてほしい…が、
        //やらないでも死ぬほどの問題ではない
        public void MoveLeftGamepadStick(Vector2 v)
        {
            if (WordToMotionDevice.Value == WordToMotionDeviceAssign.Gamepad ||
                !CheckCoolDown(ReactedHand.Left, HandTargetType.Gamepad))
            {
                return;
            }
            
            _inputEvents.RaiseMoveLeftGamepadStick(v);
        }

        public void MoveRightGamepadStick(Vector2 v)
        {
            if (WordToMotionDevice.Value == WordToMotionDeviceAssign.Gamepad ||
                !CheckCoolDown(ReactedHand.Right, HandTargetType.Gamepad))
            {
                return;
            }
            
            _inputEvents.RaiseMoveRightGamepadStick(v);
        }

        public void GamepadButtonDown(GamepadKey key)
        {
            if (WordToMotionDevice.Value == WordToMotionDeviceAssign.Gamepad)
            {
                return;
            }

            _inputEvents.RaiseGamepadButtonDown(key);
        }

        public void GamepadButtonUp(GamepadKey key)
        {
            if (WordToMotionDevice.Value == WordToMotionDeviceAssign.Gamepad)
            {
                return;
            }
            
            _inputEvents.RaiseGamepadButtonUp(key);
        }

        public void ButtonStick(Vector2Int pos)
        {
            if (WordToMotionDevice.Value == WordToMotionDeviceAssign.Gamepad || 
                !CheckCoolDown(ReactedHand.Left, HandTargetType.Gamepad))
            {
                return;
            }
            
            _inputEvents.RaiseGamepadButtonStick(pos);
        }
        
        #endregion
        
        #region Midi Controller
        
        public void KnobValueChange(int knobNumber, float value)
        {
            if (WordToMotionDevice.Value != WordToMotionDeviceAssign.MidiController)
            {
                _inputEvents.RaiseKnobValueChange(knobNumber, value);
            }
        }
        
        public void NoteOn(int noteNumber)
        {
            if (WordToMotionDevice.Value != WordToMotionDeviceAssign.MidiController)
            {
                _inputEvents.RaiseNoteOn(noteNumber);
            }
        }

        #endregion
        
        #region IHandIkGetter

        IIKData IHandIkGetter.GetLeft()
        {
            return (_currentLeftHand != null)
                ? new IKDataStruct(_currentLeftHand.Position, _currentLeftHand.Rotation)
                : new IKDataStruct(Vector3.zero, Quaternion.identity);
        }
        
        IIKData IHandIkGetter.GetRight()
        {
            return (_currentRightHand != null)
                ? new IKDataStruct(_currentRightHand.Position, _currentRightHand.Rotation)
                : new IKDataStruct(Vector3.zero, Quaternion.identity);
        }
        
        #endregion
        
        #endregion

        private void Start()
        {
            _currentRightHand = Typing.RightHand;
            _currentLeftHand = Typing.LeftHand;
            _leftHandStateBlendCount = MaxHandIkToggleDuration;
            _rightHandStateBlendCount = MaxHandIkToggleDuration;

            MouseMove.Start();
            Presentation.Start();
            GamepadHand.Start();
            MidiHand.Start();
            CarHandle.Start();
            ArcadeStickHand.Start();
        }
        
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            fingerController.Initialize(info.controlRig);
            
            //キャラロード前のHandDownとブレンドするとIK位置が原点に飛ぶので、その値を捨てる
            MouseMove.ResetHandDownTimeout(true);
            Typing.ResetLeftHandDownTimeout(true);
            Typing.ResetRightHandDownTimeout(true);
            
            //NOTE: 初期姿勢では手をおろしておく。棒立ちは何かと無難なので。
            SetRightHandState(_downHand.RightHandState);
            SetLeftHandState(_downHand.LeftHandState);
        }

        private void OnVrmDisposing()
        {
            fingerController.Dispose();
        }
        
        private void Update()
        {
            MouseMove.Update();
            Presentation.Update();
            GamepadHand.Update();
            MidiHand.Update();
            ArcadeStickHand.Update();
            CarHandle.Update();
            PenTabletHand.Update();

            //現在のステート + 必要なら直前ステートも参照してIKターゲットの位置、姿勢を更新する
            UpdateLeftHand();
            UpdateRightHand();
        }

        private void LateUpdate()
        {
            MouseMove.LateUpdate();
            GamepadHand.LateUpdate();
            MidiHand.LateUpdate();
            Presentation.LateUpdate();
            ArcadeStickHand.LateUpdate();
            PenTabletHand.LateUpdate();
        }

        private void UpdateLeftHand()
        {
            if (_leftHandIkChangeCoolDown > 0)
            {
                _leftHandIkChangeCoolDown -= Time.deltaTime;
            }

            var duration = _prevLeftHand?.TargetType == HandTargetType.ClapMotion
                ? HandIkToggleDurationAfterClap
                : HandIkToggleDuration;
            //普通の状態: 複数ステートのブレンドはせず、今のモードをそのまま通す
            if (_leftHandStateBlendCount >= duration)
            {
                _leftHandTarget.localPosition = _currentLeftHand.Position;
                _leftHandTarget.localRotation = _currentLeftHand.Rotation;
                return;
            }

            //NOTE: ここの下に来る時点では必ず_prevLeftHandに非null値が入る実装になってます

            _leftHandStateBlendCount += Time.deltaTime;
            //prevStateと混ぜるための比率
            float t = CubicEase(_leftHandStateBlendCount / duration);
            _leftHandTarget.localPosition = Vector3.Lerp(
                _prevLeftHand.Position,
                _currentLeftHand.Position,
                t
            );

            _leftHandTarget.localRotation = Quaternion.Slerp(
                _prevLeftHand.Rotation,
                _currentLeftHand.Rotation,
                t
            );
        }

        private void UpdateRightHand()
        {
            if (_rightHandIkChangeCoolDown > 0f)
            {
                _rightHandIkChangeCoolDown -= Time.deltaTime;
            }

            var duration = _prevRightHand?.TargetType == HandTargetType.ClapMotion
                ? HandIkToggleDurationAfterClap
                : HandIkToggleDuration;
            //普通の状態: 複数ステートのブレンドはせず、今のモードをそのまま通す
            if (_rightHandStateBlendCount >= duration)
            {
                _rightHandTarget.localPosition = _currentRightHand.Position;
                _rightHandTarget.localRotation = _currentRightHand.Rotation;
                return;
            }

            //NOTE: 実装上ここの下に来る時点で_prevRightHandが必ず非nullなのでnullチェックはすっ飛ばす
            
            _rightHandStateBlendCount += Time.deltaTime;
            //prevStateと混ぜるための比率
            float t = CubicEase(_rightHandStateBlendCount / duration);
            
            _rightHandTarget.localPosition = Vector3.Lerp(
                _prevRightHand.Position,
                _currentRightHand.Position,
                t
            );

            _rightHandTarget.localRotation = Quaternion.Slerp(
                _prevRightHand.Rotation,
                _currentRightHand.Rotation,
                t
            );
        }

        private bool CanChangeState(HandTargetType current, HandTargetType target)
        {
            //書いてる通りだが、
            // - 同じ状態には遷移できない
            // - 拍手は実行優先度がすごく高いので、他の状態に遷移できない
            // - 手下げモード有効時は手下げ, ハンドトラッキング, 拍手のどれかにしか遷移できない

            if (current == target)
            {
                return false;
            }

            if (current == HandTargetType.ClapMotion && ClapMotion.ClapMotionRunning)
            {
                return false;
            }
            
            if (AlwaysHandDown.Value && 
                target is not (HandTargetType.AlwaysDown or HandTargetType.ImageBaseHand or HandTargetType.ClapMotion))
            {
                return false;
            }
            
            return true; 
        }
        
        private void SetLeftHandState(IHandIkState state)
        {
            if (!CanChangeState(_leftTargetType.Value, state.TargetType))
            {
                return;
            }

            _leftTargetType.Value = state.TargetType;
            _prevLeftHand = _currentLeftHand;
            _currentLeftHand = state;
            
            _leftHandIkChangeCoolDown = HandIkTypeChangeCoolDown;
            _leftHandStateBlendCount = 0f;
            if (state.SkipEnterIkBlend)
            {
                _leftHandStateBlendCount = MaxHandIkToggleDuration;
            }

            //Stateの遷移処理。ここで指とかを更新させる
            _prevLeftHand.Quit(_currentLeftHand);
            _currentLeftHand.Enter(_prevLeftHand);
        }

        private void SetRightHandState(IHandIkState state)
        {
            if (!CanChangeState(_rightTargetType.Value, state.TargetType))
            {
                return;
            }

            _rightTargetType.Value = state.TargetType;
            _prevRightHand = _currentRightHand;
            _currentRightHand = state;
            
            _rightHandIkChangeCoolDown = HandIkTypeChangeCoolDown;
            _rightHandStateBlendCount = 0f;
            if (state.SkipEnterIkBlend)
            {
                _rightHandStateBlendCount = MaxHandIkToggleDuration;
            }

            //Stateの遷移処理。ここで指とかを更新させる
            _prevRightHand.Quit(_currentRightHand);
            _currentRightHand.Enter(_prevRightHand);
        }
        
        // NOTE: クールダウン判定をSetLeft|RightHandState時に行う手もあるが、色々考えて筋悪そうなので却下

        // クールダウンタイムを考慮したうえで、モーションを適用してよいかどうかを確認する
        private bool CheckCoolDown(ReactedHand hand, HandTargetType targetType)
        {
            if ((hand == ReactedHand.Left && targetType == _leftTargetType.Value) ||
                (hand == ReactedHand.Right && targetType == _rightTargetType.Value))
            {
                //同じデバイスを続けて触っている -> 素通しでOK
                return true;
            }

            return
                (hand == ReactedHand.Left && _leftHandIkChangeCoolDown <= 0) ||
                (hand == ReactedHand.Right && _rightHandIkChangeCoolDown <= 0);
        }
        
        // マウス/タイピングIKに関して、タイムアウトによって腕を下げていいかどうかを取得します。
        private bool CheckTypingOrMouseHandsCanMoveDown()
        {
            var left = _leftTargetType.Value;
            var right = _rightTargetType.Value;
            
            if (left != HandTargetType.Keyboard &&
                right != HandTargetType.Keyboard &&
                right != HandTargetType.Mouse)
            {
                //この場合は特に意味がない
                return false;
            }

            //NOTE: ペンタブモードの場合、ペンを持った右手+左手(キーボード上であることが多い)のいずれも降ろさせない。
            if (right == HandTargetType.PenTablet)
            {
                return false;
            }

            bool leftHandIsReady = left != HandTargetType.Keyboard || typing.LeftHandTimeOutReached;

            bool rightHandIsReady =
                (right == HandTargetType.Keyboard && typing.RightHandTimeOutReached) ||
                (right == HandTargetType.Mouse && MouseMove.IsNoInputTimeOutReached) ||
                (right != HandTargetType.Keyboard && right != HandTargetType.Mouse);

            return leftHandIsReady && rightHandIsReady;
        }

        // x in [0, 1] を y in [0, 1]へ3次補間する
        private static float CubicEase(float rate) 
            => 2 * rate * rate * (1.5f - rate);
    }

    /// <summary>
    /// 手のIKの一覧。常時手下げモードがあったり、片方の腕にしか適合しない値が入ってたりすることに注意
    /// </summary>
    public enum HandTargetType
    {
        // NOTE: 右手にのみ使う
        Mouse,
        Keyboard,
        // NOTE: 右手にのみ使う。「プレゼンモードの場合の左手」とそうでない左手はどちらもKeyboardで統一的に扱う
        Presentation,
        // NOTE: 右手にのみ使う。
        PenTablet,
        Gamepad,
        ArcadeStick,
        CarHandle,
        MidiController,
        ImageBaseHand,
        AlwaysDown,
        // NOTE: ClapMotionは「IKステートとして作られたビルトインモーション」で、他のステートに比べると一時的な使われ方をする
        ClapMotion,
        // NOTE: VMCPの適用中は他のモードに切り替わらない
        VMCPReceiveResult,
        Unknown,
    }
    
    //TODO: ここに書くのは変なので単独のスクリプト作った方が良いかもしれない。が、当面は放置でもいいかな…
    public enum WordToMotionDeviceAssign
    {
        None,
        KeyboardWord,
        KeyboardNumber,
        Gamepad,
        MidiController,
    }
        
}