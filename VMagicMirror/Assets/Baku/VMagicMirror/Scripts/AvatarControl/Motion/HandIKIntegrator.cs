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
        //NOTE: ステートパターンがめんどくさいときのステートマシンの実装です。まあステート数少ないので…

        /// <summary> IK種類が変わるときのブレンディングに使う時間。IK自体の無効化/有効化もこの時間で行う </summary>
        private const float HandIkToggleDuration = 0.25f;
        private const float HandIkTypeChangeCoolDown = 0.3f;

        [SerializeField] private TypingHandIKGenerator typing = null;
        public TypingHandIKGenerator Typing => typing;

        [SerializeField] private GamepadFingerController gamepadFinger = null;

        [SerializeField] private WaitingBodyMotion waitingBody = null;

        public MouseMoveHandIKGenerator MouseMove { get; private set; }

        public GamepadHandIKGenerator GamepadHand { get; private set; }
        
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

        public bool UseGamepadForWordToMotion { get; set; } = false;
        
        //NOTE: このフラグではキーボードのみならずマウス入力も無視することに注意
        /// <summary> NOTE: 歴史的経緯によって「受け取ってるけど使わないフラグ」になってます。 </summary>
        public bool UseKeyboardForWordToMotion { get; set; } = false;
        public bool UseMidiControllerForWordToMotion { get; set; } = false;
        
        public bool EnablePresentationMode { get; set; }

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


        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            IKTargetTransforms ikTargets, 
            Camera cam,
            ParticleStore particleStore,
            GamepadProvider gamepadProvider,
            MidiControllerProvider midiControllerProvider,
            TouchPadProvider touchPadProvider,
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
            Presentation = new PresentationHandIKGenerator(this, vrmLoadable, cam);
            _imageBaseHand = new ImageBaseHandIkGenerator(this, handTracker, imageBaseHandSetting, vrmLoadable);
            _downHand = new AlwaysDownHandIkGenerator(this, vrmLoadable);
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
        
        public void PressKey(string keyName)
        {
            if (!EnableHidArmMotion)
            {
                return;
            }
            
            var (hand, pos) = typing.PressKey(keyName, EnablePresentationMode);
            if (!CheckCoolDown(hand, HandTargetType.Keyboard))
            {
                return;
            }
            
            if (hand == ReactedHand.Left)
            {
                SetLeftHandIk(HandTargetType.Keyboard);
            }
            else if (hand == ReactedHand.Right)
            {
                SetRightHandIk(HandTargetType.Keyboard);
            }

            if (!AlwaysHandDownMode)
            {
                fingerController.StartPressKeyMotion(keyName, EnablePresentationMode);
            }
            
            if (hand != ReactedHand.None && EnableHidArmMotion)
            {
                _particleStore.RequestKeyboardParticleStart(pos);
            }
        }

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
            }
            else if (hand == ReactedHand.Right)
            {
                SetRightHandIk(HandTargetType.Keyboard);
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
            }
            else if (hand == ReactedHand.Right)
            {
                SetRightHandIk(HandTargetType.Keyboard);
            }

            if (!AlwaysHandDownMode)
            {
                fingerController.ReleaseTypingKey(keyName, EnablePresentationMode);
            }
            
            if (hand != ReactedHand.None && EnableHidArmMotion)
            {
                _particleStore.RequestKeyboardParticleStart(pos);
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
            }
        }

        public void ClickMouse(string button)
        {
            if (!EnablePresentationMode && EnableHidArmMotion && !AlwaysHandDownMode)
            {
                fingerController.OnMouseButton(button);
                SetRightHandIk(HandTargetType.Mouse);   
                if (_rightTargetType == HandTargetType.Mouse && button.Contains("Down"))
                {
                    _particleStore.RequestMouseClickParticle();
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
            
            GamepadHand.LeftStick(v);
            gamepadFinger.LeftStick(v);
            SetLeftHandIk(HandTargetType.Gamepad);
        }

        public void MoveRightGamepadStick(Vector2 v)
        {
            if (UseGamepadForWordToMotion || !CheckCoolDown(ReactedHand.Right, HandTargetType.Gamepad))
            {
                return;
            }
            GamepadHand.RightStick(v);
            gamepadFinger.RightStick(v);
            SetRightHandIk(HandTargetType.Gamepad);
        }

        public void GamepadButtonDown(GamepadKey key)
        {
            GamepadHand.ButtonDown(key);

            if (UseGamepadForWordToMotion)
            {
                return;
            }
            
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
        }

        public void GamepadButtonUp(GamepadKey key)
        {
            GamepadHand.ButtonUp(key);

            if (UseGamepadForWordToMotion)
            {
                return;
            }
            
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
            //破たんしにくくするため、指を離す方向の動作については手下げモードであってもガードしない
            gamepadFinger.ButtonUp(key);
        }

        public void ButtonStick(Vector2Int pos)
        {
            if (UseGamepadForWordToMotion)
            {
                return;
            }
            GamepadHand.ButtonStick(pos);
            SetLeftHandIk(HandTargetType.Gamepad);
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
            MidiHand.Start();
            _imageBaseHand.Start();
        }
        
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            fingerController.Initialize(info.animator);
            
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
                Typing.LeftHand;

            _prevLeftHand = _currentLeftHand;
            _currentLeftHand = ik;
            _leftHandStateBlendCount = 0f;

            if (prevType == HandTargetType.Keyboard)
            {
                //NOTE: とくにキーボード⇢ゲームパッドの遷移が破綻しないようにこのタイミングでやる
                fingerController.ReleaseLeftHandTyping();
            }
            
            if (prevType == HandTargetType.Gamepad)
            {
                gamepadFinger.ReleaseLeftHand();
            }
            if (targetType == HandTargetType.Gamepad)
            {
                gamepadFinger.GripLeftHand();
            }

            if (targetType == HandTargetType.ImageBaseHand)
            {
                _imageBaseHand.InitializeHandPosture(ReactedHand.Left, _prevLeftHand);
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

            if (prevType == HandTargetType.Gamepad)
            {
                gamepadFinger.ReleaseRightHand();
            }
            if (targetType == HandTargetType.Gamepad)
            {
                gamepadFinger.GripRightHand();
            }

            //ブレンディングをきれいにするために直前で手があった位置を拾って渡してあげる
            if (targetType == HandTargetType.ImageBaseHand)
            {
                _imageBaseHand.InitializeHandPosture(ReactedHand.Right, _prevRightHand);
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
            MidiController,
            ImageBaseHand,
            AlwaysDown,
        }

    }
}