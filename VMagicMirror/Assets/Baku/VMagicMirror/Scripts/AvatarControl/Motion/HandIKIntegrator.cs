using System;
using Baku.VMagicMirror.IK;
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
        private ParticleStore _particleStore = null;

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


        public bool UseGamepadForWordToMotion { get; set; } = false;

        //NOTE: このフラグではキーボードのみならずマウス入力も無視することに注意
        /// <summary> NOTE: 歴史的経緯によって「受け取ってるけど使わないフラグ」になってます。 </summary>
        public bool UseKeyboardForWordToMotion { get; set; } = false;

        public bool UseMidiControllerForWordToMotion { get; set; } = false;

        public bool EnablePresentationMode { get; set; }
        
        public void SetKeyboardAndMouseMotionMode(int modeIndex)
        {
            //TODO: 実装
        }

        public void SetGamepadMotionMode(int modeIndex)
        {
            if (modeIndex >= 0 &&
                modeIndex <= (int) GamepadMotionModes.Unknown &&
                modeIndex != (int) _gamepadMotionMode
                //DEBUG: とりあえずアケコンと通常ゲームパッドだけやる
                && (modeIndex == 0 || modeIndex == 1)
                )
            {   
                _gamepadMotionMode = (GamepadMotionModes) modeIndex;
                //NOTE: オプションを変えた直後は手はとりあえず動かさないでおく(変更後の入力によって手が動く)。
                //変えた時点で切り替わるほうが嬉しい、と思ったらそういう挙動に直す
            }
        }

        private GamepadMotionModes _gamepadMotionMode = GamepadMotionModes.Gamepad;
        
        //NOTE: これはすごく特別なフラグで、これが立ってると手のIKに何か入った場合でも手が下がりっぱなしになります
        private bool _alwaysHandDownMode = false;
        public bool AlwaysHandDownMode
        {
            get => _alwaysHandDownMode;
            set
            {
                _alwaysHandDownMode = value;                
                //NOTE: フラグが折れた場合、そのあとの入力に基づいてIKが変わるのに任せる
                if (!value)
                {
                    return;
                }
                SetLeftHandIk(HandTargetType.AlwaysDown);
                SetRightHandIk(HandTargetType.AlwaysDown);
            }
        }

        public bool IsLeftHandGripGamepad => _leftTargetType == HandTargetType.Gamepad;
        public bool IsRightHandGripGamepad => _rightTargetType == HandTargetType.Gamepad;

        public Vector3 RightHandPosition => _rightHandTarget.position;
        public Vector3 LeftHandPosition => _leftHandTarget.position;

        #endregion

        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            IKTargetTransforms ikTargets, 
            Camera cam,
            ParticleStore particleStore,
            GamepadProvider gamepadProvider,
            MidiControllerProvider midiControllerProvider,
            TouchPadProvider touchPadProvider,
            ArcadeStickProvider arcadeStickProvider,
            HandTracker handTracker
            )
        {
            _rightHandTarget = ikTargets.RightHand;
            _leftHandTarget = ikTargets.LeftHand;
            _particleStore = particleStore;
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;        

            MouseMove = new MouseMoveHandIKGenerator(this, touchPadProvider);
            MidiHand = new MidiHandIkGenerator(this, midiControllerProvider);
            GamepadHand = new GamepadHandIKGenerator(
                this, vrmLoadable, waitingBody, gamepadProvider, gamepadSetting
                );
            ArcadeStickHand = new ArcadeStickHandIKGenerator(this, vrmLoadable, arcadeStickProvider);
            Presentation = new PresentationHandIKGenerator(this, vrmLoadable, cam);
            _imageBaseHand = new ImageBaseHandIkGenerator(this, handTracker, imageBaseHandSetting, vrmLoadable);
            _downHand = new AlwaysDownHandIkGenerator(this, vrmLoadable);

            MouseMove.DownHand = _downHand;
            MouseMove.Integrator = this;
            typing.DownHand = _downHand;
            typing.Integrator = this;

        }

        //NOTE: 初めて手がキーボードから離れるまではnull
        private IIKGenerator _prevRightHand = null;

        //NOTE: Start以降はnullにならない
        private IIKGenerator _currentRightHand = null;

        private IIKGenerator _prevLeftHand = null;
        private IIKGenerator _currentLeftHand = null;

        private HandTargetType _leftTargetType = HandTargetType.Keyboard;
        private HandTargetType _rightTargetType = HandTargetType.Keyboard;

        #region API

        #region Keyboard and Mouse
        
        public void KeyDown(string keyName)
        {
            if (!EnableHidArmMotion)
            {
                return;
            }

            var (hand, pos) = typing.KeyDown(keyName, EnablePresentationMode);
            if (!CheckCoolDown(hand, HandTargetType.Keyboard))
            {
                return;
            }
            
            if (hand == ReactedHand.Left)
            {
                SetLeftHandIk(HandTargetType.Keyboard);
                if (_leftTargetType == HandTargetType.Keyboard)
                {
                    typing.ResetLeftHandDownTimeout(false);
                }
            }
            else if (hand == ReactedHand.Right)
            {
                SetRightHandIk(HandTargetType.Keyboard);
                if (_rightTargetType == HandTargetType.Keyboard)
                {
                    typing.ResetRightHandDownTimeout(false);
                }
            }

            if (!AlwaysHandDownMode)
            {
                fingerController.HoldTypingKey(keyName, EnablePresentationMode);
            }
            
            if (hand != ReactedHand.None && EnableHidArmMotion)
            {
                _particleStore.RequestKeyboardParticleStart(pos);
            }
        }

        public void KeyUp(string keyName)
        {
            if (!EnableHidArmMotion)
            {
                return;
            }
            
            var (hand, pos) = typing.KeyUp(keyName, EnablePresentationMode);
            if (!CheckCoolDown(hand, HandTargetType.Keyboard))
            {
                return;
            }
            
            if (hand == ReactedHand.Left)
            {
                SetLeftHandIk(HandTargetType.Keyboard);
                if (_leftTargetType == HandTargetType.Keyboard)
                {
                    typing.ResetLeftHandDownTimeout(false);
                }
            }
            else if (hand == ReactedHand.Right)
            {
                SetRightHandIk(HandTargetType.Keyboard);
                if (_rightTargetType == HandTargetType.Keyboard)
                {
                    typing.ResetRightHandDownTimeout(false);
                }
            }

            if (!AlwaysHandDownMode)
            {
                fingerController.ReleaseTypingKey(keyName, EnablePresentationMode);
            }
        }
        

        public void MoveMouse(Vector3 mousePosition)
        {
            if (!EnableHidArmMotion)
            {
                return;
            }
            
            if (!CheckCoolDown(
                ReactedHand.Right, 
                EnablePresentationMode ? HandTargetType.Presentation : HandTargetType.Mouse
                ))
            {
                return;
            }
            
            Presentation.MoveMouse(mousePosition);
            SetRightHandIk(EnablePresentationMode ? HandTargetType.Presentation : HandTargetType.Mouse);
            if (_rightTargetType == HandTargetType.Mouse)
            {
                _particleStore.RequestMouseMoveParticle(MouseMove.ReferenceTouchpadPosition);
                MouseMove.ResetHandDownTimeout(false);
            }
        }

        public void OnMouseButton(string button)
        {
            if (!EnablePresentationMode && EnableHidArmMotion && !AlwaysHandDownMode)
            {
                fingerController.OnMouseButton(button);
                SetRightHandIk(HandTargetType.Mouse);   
                
                //マウスはButtonUpでもエフェクトを出す。
                //ちょっとうるさくなるが、意味的にはMouseのButtonUpはけっこうデカいアクションなので
                if (_rightTargetType == HandTargetType.Mouse)
                {
                    _particleStore.RequestMouseClickParticle();
                    MouseMove.ResetHandDownTimeout(false);
                }
            }
        }

        #endregion
        
        #region Gamepad
        
        //NOTE: 表情コントロール用にゲームパッドを使っている間は入力を無視する
        
        public void MoveLeftGamepadStick(Vector2 v)
        {
            if (UseGamepadForWordToMotion || !CheckCoolDown(ReactedHand.Left, HandTargetType.Gamepad))
            {
                return;
            }

            switch (_gamepadMotionMode)
            {
                case GamepadMotionModes.Gamepad:
                    GamepadHand.LeftStick(v);
                    gamepadFinger.LeftStick(v);
                    SetLeftHandIk(HandTargetType.Gamepad);
                    break;
                case GamepadMotionModes.ArcadeStick:
                    ArcadeStickHand.LeftStick(v);
                    SetLeftHandIk(HandTargetType.ArcadeStick);
                    break;
                default:
                    break;
            }
        }

        public void MoveRightGamepadStick(Vector2 v)
        {
            if (UseGamepadForWordToMotion || !CheckCoolDown(ReactedHand.Right, HandTargetType.Gamepad))
            {
                return;
            }

            switch (_gamepadMotionMode)
            {
                case GamepadMotionModes.Gamepad:
                    GamepadHand.RightStick(v);
                    gamepadFinger.RightStick(v);
                    SetRightHandIk(HandTargetType.Gamepad);
                    break;
                default:
                    break;
            }
        }

        public void GamepadButtonDown(GamepadKey key)
        {
            GamepadHand.ButtonDown(key);
            ArcadeStickHand.ButtonDown(key);

            if (UseGamepadForWordToMotion)
            {
                return;
            }

            switch (_gamepadMotionMode)
            {
                case GamepadMotionModes.Gamepad:
                    var hand = GamepadProvider.GetPreferredReactionHand(key);
                    if (hand == ReactedHand.Left)
                    {
                        SetLeftHandIk(HandTargetType.Gamepad);
                    }
                    else if (hand == ReactedHand.Right)
                    {
                        SetRightHandIk(HandTargetType.Gamepad);
                    }

                    if (!AlwaysHandDownMode)
                    {
                        gamepadFinger.ButtonDown(key);
                    }
                    break;
                case GamepadMotionModes.ArcadeStick:
                    SetRightHandIk(HandTargetType.ArcadeStick);
                    if (!AlwaysHandDownMode)
                    {
                        arcadeStickFinger.ButtonDown(key);
                    }
                    break;
                default:
                    break;
            }
            
        }

        public void GamepadButtonUp(GamepadKey key)
        {
            GamepadHand.ButtonUp(key);
            ArcadeStickHand.ButtonUp(key);

            if (UseGamepadForWordToMotion)
            {
                return;
            }

            switch (_gamepadMotionMode)
            {
                case GamepadMotionModes.Gamepad:
                    var hand = GamepadProvider.GetPreferredReactionHand(key);
                    if (hand == ReactedHand.Left)
                    {
                        SetLeftHandIk(HandTargetType.Gamepad);
                    }
                    else if (hand == ReactedHand.Right)
                    {
                        SetRightHandIk(HandTargetType.Gamepad);
                    }
                    
                    //NOTE: めっちゃ起きにくいが、「コントローラのボタンを押したまま手さげモードに入る」というケースを
                    //破たんしにくくするため、指を離す方向の動作については手下げモードであってもガードしない。
                    //この次のアケコンについても同じ考え方
                    gamepadFinger.ButtonUp(key);
                    break;
                case GamepadMotionModes.ArcadeStick:
                    arcadeStickFinger.ButtonUp(key);
                    break;
                default:
                    break;
            }
            
        }

        public void ButtonStick(Vector2Int pos)
        {
            if (UseGamepadForWordToMotion)
            {
                return;
            }

            switch (_gamepadMotionMode)
            {
                case GamepadMotionModes.Gamepad:
                    GamepadHand.ButtonStick(pos);
                    SetLeftHandIk(HandTargetType.Gamepad);
                    break;
                case GamepadMotionModes.ArcadeStick:
                    ArcadeStickHand.ButtonStick(pos);
                    SetLeftHandIk(HandTargetType.ArcadeStick);
                    break;
                default:
                    break;
            }
        }
        
        #endregion
        
        #region Midi Controller
        
        public void KnobValueChange(int knobNumber, float value)
        {
            if (UseMidiControllerForWordToMotion)
            {
                return;
            }
            
            var hand = MidiHand.KnobValueChange(knobNumber, value);
            if (hand == ReactedHand.Left)
            {
                SetLeftHandIk(HandTargetType.MidiController);
            }
            else
            {
                SetRightHandIk(HandTargetType.MidiController);
            }
        }
        
        public void NoteOn(int noteNumber)
        {
            if (UseMidiControllerForWordToMotion)
            {
                return;
            }
            
            var (hand, pos) = MidiHand.NoteOn(noteNumber);
            if (hand == ReactedHand.Left)
            {
                SetLeftHandIk(HandTargetType.MidiController);
            }
            else
            {
                SetRightHandIk(HandTargetType.MidiController);
            }
            _particleStore.RequestMidiParticleStart(pos);
        }

        
        #endregion
        
        #region Image Base Hand

        //画像処理の手検出があったらそっちのIKに乗り換える
        private void ExecuteOrCheckHandUpdates()
        {
            MouseMove.Update();
            Presentation.Update();
            GamepadHand.Update();
            ArcadeStickHand.Update();
            MidiHand.Update();
            _imageBaseHand.Update();
            
            if (_imageBaseHand.HasRightHandUpdate)
            {
                _imageBaseHand.HasRightHandUpdate = false;
                SetRightHandIk(HandTargetType.ImageBaseHand);
            }

            if (_imageBaseHand.HasLeftHandUpdate)
            {
                _imageBaseHand.HasLeftHandUpdate = false;
                SetLeftHandIk(HandTargetType.ImageBaseHand);
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
            if (_leftTargetType != HandTargetType.Keyboard &&
                _rightTargetType != HandTargetType.Keyboard &&
                _rightTargetType != HandTargetType.Mouse)
            {
                //この場合は特に意味がない
                return false;
            }

            bool leftHandIsReady =
                _leftTargetType != HandTargetType.Keyboard ||
                typing.LeftHandTimeOutReached;

            bool rightHandIsReady =
                (_rightTargetType == HandTargetType.Keyboard && typing.RightHandTimeOutReached) ||
                (_rightTargetType == HandTargetType.Mouse && MouseMove.IsNoInputTimeOutReached) ||
                (_rightTargetType != HandTargetType.Keyboard && _rightTargetType != HandTargetType.Mouse);

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
            //棒立ちサポートをめちゃ適当にやっちゃえ！というのがモチベです
            _imageBaseHand.HasRightHandUpdate = false;
            SetRightHandIk(HandTargetType.ImageBaseHand);

            _imageBaseHand.HasLeftHandUpdate = false;
            SetLeftHandIk(HandTargetType.ImageBaseHand);
        }

        private void OnVrmDisposing()
        {
            fingerController.Dispose();
        }
        
        private void Update()
        {
            ExecuteOrCheckHandUpdates();
            
            //ねらい: 前のステートと今のステートをブレンドしながら実際にIKターゲットの位置、姿勢を更新する
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

        //TODO: IKオン/オフとの兼ね合いがアレなのでどうにかしてね。

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

        //TODO: Stateパターンをそろそろ検討しないとヤバそう

        private void SetLeftHandIk(HandTargetType targetType)
        {
            if (_leftTargetType == targetType)
            {
                return;
            }
            else if (_alwaysHandDownMode && targetType != HandTargetType.AlwaysDown)
            {
                //手下げっぱなしモードに入った場合、他のIKには遷移できない
                return;
            }

            _leftHandIkChangeCoolDown = HandIkTypeChangeCoolDown;

            var prevType = _leftTargetType;
            _leftTargetType = targetType;

            var ik =
                (targetType == HandTargetType.Keyboard) ? Typing.LeftHand :
                (targetType == HandTargetType.Gamepad) ? GamepadHand.LeftHand :
                (targetType == HandTargetType.MidiController) ? MidiHand.LeftHand : 
                (targetType == HandTargetType.ImageBaseHand) ? _imageBaseHand.LeftHand :
                (targetType == HandTargetType.AlwaysDown) ? _downHand.LeftHand :
                (targetType == HandTargetType.ArcadeStick) ? ArcadeStickHand.LeftHand : 
                Typing.LeftHand;

            _prevLeftHand = _currentLeftHand;
            _currentLeftHand = ik;
            _leftHandStateBlendCount = 0f;

            switch (prevType)
            {
                case HandTargetType.Keyboard:
                    //NOTE: とくにキーボード⇢ゲームパッドの遷移が破綻しないようにこのタイミングでやる
                    fingerController.ReleaseLeftHandTyping();
                    break;
                case HandTargetType.Gamepad:
                    gamepadFinger.ReleaseLeftHand();
                    break;
                case HandTargetType.ArcadeStick:
                    arcadeStickFinger.ReleaseLeftHand();
                    break;
            }

            switch (targetType)
            {
                case HandTargetType.Keyboard:
                    typing.ResetLeftHandDownTimeout(true);
                    break;
                case HandTargetType.Gamepad:
                    gamepadFinger.GripLeftHand();
                    break;
                case HandTargetType.ArcadeStick:
                    arcadeStickFinger.GripLeftHand();
                    break;
                case HandTargetType.ImageBaseHand:
                    _imageBaseHand.InitializeHandPosture(ReactedHand.Left, _prevLeftHand);
                    break;
            }
            
            GamepadHand.HandIsOnController = 
                _leftTargetType == HandTargetType.Gamepad ||
                _rightTargetType == HandTargetType.Gamepad;
        }

        private void SetRightHandIk(HandTargetType targetType)
        {
            if (_rightTargetType == targetType)
            {
                return;
            }
            else if (_alwaysHandDownMode && targetType != HandTargetType.AlwaysDown)
            {
                //手下げっぱなしモードに入った場合、他のIKには遷移できない
                return;
            }

            _rightHandIkChangeCoolDown = HandIkTypeChangeCoolDown;

            var prevType = _rightTargetType;
            _rightTargetType = targetType;

            var ik =
                (targetType == HandTargetType.Mouse) ? MouseMove.RightHand :
                (targetType == HandTargetType.Keyboard) ? Typing.RightHand :
                (targetType == HandTargetType.Gamepad) ? GamepadHand.RightHand :
                (targetType == HandTargetType.Presentation) ? Presentation.RightHand :
                (targetType == HandTargetType.MidiController) ? MidiHand.RightHand :
                (targetType == HandTargetType.ImageBaseHand) ? _imageBaseHand.RightHand :
                (targetType == HandTargetType.AlwaysDown) ? _downHand.RightHand :
                (targetType == HandTargetType.ArcadeStick) ? ArcadeStickHand.RightHand :
                Typing.RightHand;

            _prevRightHand = _currentRightHand;
            _currentRightHand = ik;
            _rightHandStateBlendCount = 0f;

            fingerController.RightHandPresentationMode = (targetType == HandTargetType.Presentation);

            //NOTE: マウスの指はタイピング動作と共通の方式なため、これらは同じ仕組みで指を離す。
            //ただしマウスからキーボードに行く場合だけはReleaseを呼ばないでもちゃんと動くので、あえて呼ばない
            if (prevType == HandTargetType.Keyboard || 
                (prevType == HandTargetType.Mouse && targetType != HandTargetType.Keyboard))
            {
                fingerController.ReleaseRightHandTyping();
            }

            switch (prevType)
            {
                case HandTargetType.Gamepad:
                    gamepadFinger.ReleaseRightHand();
                    break;
                case HandTargetType.ArcadeStick:
                    arcadeStickFinger.ReleaseRightHand();
                    break;
            }

            switch (targetType)
            {
                case HandTargetType.Keyboard:
                    typing.ResetRightHandDownTimeout(true);
                    break;
                case HandTargetType.Mouse:
                    MouseMove.ResetHandDownTimeout(true);
                    break;
                case HandTargetType.Gamepad:
                    gamepadFinger.GripRightHand();
                    break;
                case HandTargetType.ArcadeStick:
                    arcadeStickFinger.GripRightHand();
                    break;
                case HandTargetType.ImageBaseHand:
                    //ブレンディングをきれいにするために直前で手があった位置を拾って渡してあげる
                    _imageBaseHand.InitializeHandPosture(ReactedHand.Right, _prevRightHand);
                    break;
            }

            GamepadHand.HandIsOnController = 
                _leftTargetType == HandTargetType.Gamepad ||
                _rightTargetType == HandTargetType.Gamepad;
        }

        //クールダウンタイムを考慮したうえで、モーションを適用してよいかどうかを確認します。
        private bool CheckCoolDown(ReactedHand hand, HandTargetType targetType)
        {
            if ((hand == ReactedHand.Left && targetType == _leftTargetType) ||
                (hand == ReactedHand.Right && targetType == _rightTargetType)
            )
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
        
        enum HandTargetType
        {
            Mouse,
            Keyboard,
            Presentation,
            Gamepad,
            ArcadeStick,
            MidiController,
            ImageBaseHand,
            AlwaysDown,
        }

        /// <summary>
        /// ゲームパッド由来のモーションをどういう見た目で反映するか、というオプション。
        /// </summary>
        /// <remarks>
        /// どれを選んでいるにせよ、Word to Motionをゲームパッドでやっている間は処理が止まるなどの基本的な特徴は共通
        /// </remarks>
        enum GamepadMotionModes
        {
            /// <summary> 普通のゲームパッド </summary>
            Gamepad = 0,
            /// <summary> アケコン </summary>
            ArcadeStick = 1,
            /// <summary> ガンコン </summary>
            GunController = 2,
            /// <summary> 車のハンドルっぽいやつ </summary>
            CarController = 3,
            Unknown = 4,
        }
        
    }
}