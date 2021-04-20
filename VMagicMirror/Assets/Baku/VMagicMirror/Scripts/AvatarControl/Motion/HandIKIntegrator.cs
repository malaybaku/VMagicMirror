using Baku.VMagicMirror.IK;
using UniRx;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// ユーザーの入力と設定に基づいて、実際にIKを適用していくやつ
    /// </summary>
    public class HandIKIntegrator : MonoBehaviour
    {
        #region settings
        
        //NOTE: ステートパターンがめんどくさいときのステートマシンの実装です。まあステート数少ないので…

        /// <summary> IK種類が変わるときのブレンディングに使う時間。IK自体の無効化/有効化もこの時間で行う </summary>
        private const float HandIkToggleDuration = 0.25f;

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

        public MouseMoveHandIKGenerator MouseMove { get; private set; }

        public GamepadHandIKGenerator GamepadHand { get; private set; }

        public ArcadeStickHandIKGenerator ArcadeStickHand { get; private set; }

        public MidiHandIkGenerator MidiHand { get; private set; }

        public PresentationHandIKGenerator Presentation { get; private set; }

        private ImageBaseHandIkGenerator _imageBaseHand;

        private AlwaysDownHandIkGenerator _downHand;


        [SerializeField] private FingerController fingerController = null;

        [SerializeField] private GamepadHandIKGenerator.GamepadHandIkGeneratorSetting gamepadSetting = default;

        [SerializeField]
        private ImageBaseHandIkGenerator.ImageBaseHandIkGeneratorSetting imageBaseHandSetting = default;

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
            new ReactiveProperty<WordToMotionDeviceAssign>(WordToMotionDeviceAssign.KeyboardWord);
        
        //TODO: ↓の2つを消して上のWordToMotionDeviceに帰着させる
        // public bool UseGamepadForWordToMotion { get; set; } = false;
        // public bool UseMidiControllerForWordToMotion { get; set; } = false;

        public bool EnablePresentationMode => _keyboardAndMouseMotionMode.Value == KeyboardAndMouseMotionModes.Presentation;
        
        public void SetKeyboardAndMouseMotionMode(int modeIndex)
        {
            if (modeIndex >= 0 &&
                modeIndex <= (int) KeyboardAndMouseMotionModes.Unknown &&
                modeIndex != (int) _keyboardAndMouseMotionMode.Value
                //DEBUG: とりあえず通常モードとプレゼンだけ考慮
                && (modeIndex == 0 || modeIndex == 1)
            )
            {
                //NOTE: オプションを変えた直後は手は動かさず、変更後の入力によって手が動く
                _keyboardAndMouseMotionMode.Value = (KeyboardAndMouseMotionModes) modeIndex;
            }
        }

        public void SetGamepadMotionMode(int modeIndex)
        {
            if (modeIndex >= 0 &&
                modeIndex <= (int) GamepadMotionModes.Unknown &&
                modeIndex != (int) _gamepadMotionMode.Value
                //DEBUG: とりあえずアケコンと通常ゲームパッドだけやる
                && (modeIndex == 0 || modeIndex == 1)
                )
            {   
                _gamepadMotionMode.Value = (GamepadMotionModes) modeIndex;
                //NOTE: オプションを変えた直後は手はとりあえず動かさないでおく(変更後の入力によって手が動く)。
                //変えた時点で切り替わるほうが嬉しい、と思ったらそういう挙動に直す
            }
        }

        private readonly ReactiveProperty<GamepadMotionModes> _gamepadMotionMode =
            new ReactiveProperty<GamepadMotionModes>(GamepadMotionModes.Gamepad);

        private readonly ReactiveProperty<KeyboardAndMouseMotionModes> _keyboardAndMouseMotionMode = 
            new ReactiveProperty<KeyboardAndMouseMotionModes>(KeyboardAndMouseMotionModes.KeyboardAndTouchPad);
        
        //NOTE: これはすごく特別なフラグで、これが立ってると手のIKに何か入った場合でも手が下がりっぱなしになる
        public ReactiveProperty<bool> AlwaysHandDown { get; } = new ReactiveProperty<bool>(false);

        public bool IsLeftHandGripGamepad => _leftTargetType.Value == HandTargetType.Gamepad;
        public bool IsRightHandGripGamepad => _rightTargetType.Value == HandTargetType.Gamepad;
        
        public Vector3 RightHandPosition => _rightHandTarget.position;
        public Vector3 LeftHandPosition => _leftHandTarget.position;

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
            HandTracker handTracker
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
                this, _reactionSources, runtimeConfig, _inputEvents
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
            ArcadeStickHand = new ArcadeStickHandIKGenerator(dependency, vrmLoadable, arcadeStickProvider);
            Presentation = new PresentationHandIKGenerator(dependency, vrmLoadable, cam);
            _imageBaseHand = new ImageBaseHandIkGenerator(dependency, handTracker, imageBaseHandSetting, vrmLoadable);
            _downHand = new AlwaysDownHandIkGenerator(dependency, vrmLoadable);
            typing.SetUp(keyboardProvider, dependency);

            MouseMove.DownHand = _downHand;
            typing.DownHand = _downHand;

            //TODO: TypingだけMonoBehaviourなせいで若干ダサい
            foreach (var generator in new HandIkGeneratorBase[]
                {
                    MouseMove, MidiHand, GamepadHand, ArcadeStickHand, Presentation, _imageBaseHand, _downHand
                })
            {
                if (generator.LeftHandState != null)
                {
                    generator.LeftHandState.RequestToUse += SetLeftHandIk;
                }

                if (generator.RightHandState != null)
                {
                    generator.RightHandState.RequestToUse += SetRightHandIk;
                }
            }
            
            Typing.LeftHand.RequestToUse += SetLeftHandIk;
            Typing.RightHand.RequestToUse += SetRightHandIk;
        }

        //NOTE: prevのStateは初めて手がキーボードから離れるまではnull
        private IHandIkState _prevRightHand = null;
        private IHandIkState _prevLeftHand = null;

        //NOTE: こっちはStart()以降は非null
        private IHandIkState _currentRightHand = null;
        private IHandIkState _currentLeftHand = null;

        //NOTE: 値自体はCurrentRightHand.TargetTypeとかと等しい。値を他のIKに露出するために使う
        private readonly ReactiveProperty<HandTargetType> _leftTargetType 
            = new ReactiveProperty<HandTargetType>(HandTargetType.Keyboard);
        private readonly ReactiveProperty<HandTargetType> _rightTargetType
            = new ReactiveProperty<HandTargetType>(HandTargetType.Keyboard);

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
            if (!EnableHidArmMotion ||
                !CheckCoolDown(
                    ReactedHand.Right, 
                    EnablePresentationMode ? HandTargetType.Presentation : HandTargetType.Mouse
                    ))
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
        
        //NOTE: 表情コントロール用にゲームパッドを使っている間は入力を無視する
        //TODO: ButtonDown/ButtonUpについても可能ならCooldownチェックのガードに相当するものをしたい。
        //HandIk側でやってもらうしかないかな…？
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
            //TODO: たぶんデグレしないが、ガードがやや厳しい方向に変化してるので動作の変化に要注意
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
        
        #region Util

        /// <summary>
        /// マウス/タイピングIKに関して、タイムアウトによって腕を下げていいかどうかを取得します。
        /// </summary>
        /// <returns></returns>
        public bool CheckTypingOrMouseHandsCanMoveDown()
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

            bool leftHandIsReady = left != HandTargetType.Keyboard || typing.LeftHandTimeOutReached;

            bool rightHandIsReady =
                (right == HandTargetType.Keyboard && typing.RightHandTimeOutReached) ||
                (right == HandTargetType.Mouse && MouseMove.IsNoInputTimeOutReached) ||
                (right != HandTargetType.Keyboard && right != HandTargetType.Mouse);

            return leftHandIsReady && rightHandIsReady;
        }
        
        #endregion
        
        #endregion

        private void Start()
        {
            _currentRightHand = Typing.RightHand;
            _currentLeftHand = Typing.LeftHand;
            _leftHandStateBlendCount = HandIkToggleDuration;
            _rightHandStateBlendCount = HandIkToggleDuration;

            MouseMove.Start();
            Presentation.Start();
            GamepadHand.Start();
            ArcadeStickHand.Start();
            MidiHand.Start();
            _imageBaseHand.Start();
        }
        
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            fingerController.Initialize(info.animator);
            
            //キャラロード前の時点ではHandDownとブレンドされることでIK位置が原点に飛ぶため、それらの値を捨てる
            MouseMove.ResetHandDownTimeout(true);
            Typing.ResetLeftHandDownTimeout(true);
            Typing.ResetRightHandDownTimeout(true);
            
            //NOTE: 初期姿勢は「トラッキングできてない(はずの)画像ベースハンドトラッキングのやつ」にします。
            //こうすると棒立ちになるので都合がよいです
            _imageBaseHand.HasRightHandUpdate = false;
            SetRightHandIk(_imageBaseHand.RightHandState);
            _imageBaseHand.HasLeftHandUpdate = false;
            SetLeftHandIk(_imageBaseHand.LeftHandState);
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
            ArcadeStickHand.Update();
            MidiHand.Update();
            _imageBaseHand.Update();

            //画像処理の手検出があったらそっちのIKに乗り換える
            if (_imageBaseHand.HasRightHandUpdate)
            {
                _imageBaseHand.HasRightHandUpdate = false;
                SetRightHandIk(_imageBaseHand.RightHandState);
            }

            if (_imageBaseHand.HasLeftHandUpdate)
            {
                _imageBaseHand.HasLeftHandUpdate = false;
                SetLeftHandIk(_imageBaseHand.LeftHandState);
            }
            
            //現在のステート + 必要なら直前ステートも参照してIKターゲットの位置、姿勢を更新する
            UpdateLeftHand();
            UpdateRightHand();
        }

        private void LateUpdate()
        {
            MouseMove.LateUpdate();
            GamepadHand.LateUpdate();
            ArcadeStickHand.LateUpdate();
            MidiHand.LateUpdate();
            Presentation.LateUpdate();
            _imageBaseHand.LateUpdate();
        }
        
        private void UpdateLeftHand()
        {
            if (_leftHandIkChangeCoolDown > 0)
            {
                _leftHandIkChangeCoolDown -= Time.deltaTime;
            }
            
            //普通の状態: 複数ステートのブレンドはせず、今のモードをそのまま通す
            if (_leftHandStateBlendCount >= HandIkToggleDuration)
            {
                _leftHandTarget.localPosition = _currentLeftHand.Position;
                _leftHandTarget.localRotation = _currentLeftHand.Rotation;
                return;
            }

            //NOTE: ここの下に来る時点では必ず_prevLeftHandに非null値が入る実装になってます

            _leftHandStateBlendCount += Time.deltaTime;
            //prevStateと混ぜるための比率
            float t = CubicEase(_leftHandStateBlendCount / HandIkToggleDuration);
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
            
            //普通の状態: 複数ステートのブレンドはせず、今のモードをそのまま通す
            if (_rightHandStateBlendCount >= HandIkToggleDuration)
            {
                _rightHandTarget.localPosition = _currentRightHand.Position;
                _rightHandTarget.localRotation = _currentRightHand.Rotation;
                return;
            }

            //NOTE: 実装上ここの下に来る時点で_prevRightHandが必ず非nullなのでnullチェックはすっ飛ばす
            
            _rightHandStateBlendCount += Time.deltaTime;
            //prevStateと混ぜるための比率
            float t = CubicEase(_rightHandStateBlendCount / HandIkToggleDuration);
            
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
        
        private void SetLeftHandIk(IHandIkState state)
        {
            var targetType = state.TargetType;
            
            if (_leftTargetType.Value == targetType || 
                (AlwaysHandDown.Value && targetType != HandTargetType.AlwaysDown))
            {
                //書いてる通りだが、同じ状態には遷移できない + 手下げモードのときは他のモードにならない
                return;
            }

            _leftHandIkChangeCoolDown = HandIkTypeChangeCoolDown;

            var prevType = _leftTargetType.Value;
            _leftTargetType.Value = targetType;
            
            _prevLeftHand = _currentLeftHand;
            _currentLeftHand = state;
            _leftHandStateBlendCount = 0f;

            switch (prevType)
            {
                case HandTargetType.Keyboard:
                    //NOTE: とくにキーボード⇢ゲームパッドの遷移が破綻しないようにこのタイミングでやる
                    fingerController.ReleaseLeftHandTyping();
                    break;
            }

            switch (targetType)
            {
                case HandTargetType.Keyboard:
                    typing.ResetLeftHandDownTimeout(true);
                    break;
            }
        }

        private void SetRightHandIk(IHandIkState state)
        {
            var targetType = state.TargetType;
            
            if (_rightTargetType.Value == targetType || 
                (AlwaysHandDown.Value && targetType != HandTargetType.AlwaysDown))
            {
                //書いてる通りだが、同じ状態には遷移できない + 手下げモードのときは他のモードにならない
                return;
            }

            _rightHandIkChangeCoolDown = HandIkTypeChangeCoolDown;

            var prevType = _rightTargetType.Value;
            _rightTargetType.Value = targetType;
            
            _prevRightHand = _currentRightHand;
            _currentRightHand = state;
            _rightHandStateBlendCount = 0f;
            
            //NOTE: 除外しているパターンマウスの指はタイピング動作と共通の方式なため、これらは同じ仕組みで指を離す。
            //ただしマウスからキーボードに行く場合だけはReleaseを呼ばないでもちゃんと動くので、あえて呼ばない
            if (prevType == HandTargetType.Keyboard)
            {
                fingerController.ReleaseRightHandTyping();
            }

            switch (targetType)
            {
                case HandTargetType.Keyboard:
                    typing.ResetRightHandDownTimeout(true);
                    break;
            }
        }
        
        //TODO: クールダウンの判定はSetLeft|RightHandIKのガード時に行うのではダメか？

        //クールダウンタイムを考慮したうえで、モーションを適用してよいかどうかを確認します。
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
        
        /// <summary>
        /// x in [0, 1] を y in [0, 1]へ3次補間するやつ
        /// </summary>
        /// <param name="rate"></param>
        /// <returns></returns>
        private static float CubicEase(float rate) 
            => 2 * rate * rate * (1.5f - rate);
    }


    
    /// <summary>
    /// 手のIKの一覧。常時手下げモードがあったり、片方の腕にしか適合しない値が入ってたりすることに注意
    /// </summary>
    public enum HandTargetType
    {
        Mouse,
        Keyboard,
        /// <summary>
        /// TODO: 「プレゼン中の左手」はこの値で表現すべき…？実際に触るのはキーボードなんだけど
        /// </summary>
        Presentation,
        Gamepad,
        ArcadeStick,
        MidiController,
        ImageBaseHand,
        AlwaysDown,
    }
    
    //TODO: ここに書くのは変なので単独のスクリプト作った方がいいような…
    public enum WordToMotionDeviceAssign
    {
        None,
        KeyboardWord,
        KeyboardNumber,
        Gamepad,
        MidiController,
    }
        
}