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

        [SerializeField] private MouseMoveHandIKGenerator mouseMove = null;
        public MouseMoveHandIKGenerator MouseMove => mouseMove;

        public GamepadHandIKGenerator GamepadHand { get; private set; }
        
        public MidiHandIkGenerator MidiHand { get; private set; }

        [SerializeField] private PresentationHandIKGenerator presentation = null;
        public PresentationHandIKGenerator Presentation => presentation;

        [SerializeField] private ImageBaseHandIkGenerator imageBaseHand = null;

        [SerializeField] private FingerController fingerController = null;

        [SerializeField] private GamepadHandIKGenerator.GamepadHandIkGeneratorSetting gamepadSetting = default;

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
                mouseMove.EnableUpdate = value;
            }
        }

        public bool UseGamepadForWordToMotion { get; set; } = false;
        
        //NOTE: このフラグではキーボードのみならずマウス入力も無視することに注意
        /// <summary> NOTE: 歴史的経緯によって「受け取ってるけど使わないフラグ」になってます。 </summary>
        public bool UseKeyboardForWordToMotion { get; set; } = false;
        public bool UseMidiControllerForWordToMotion { get; set; } = false;
        
        public bool EnablePresentationMode { get; set; }

        public bool IsLeftHandGripGamepad => _leftTargetType == HandTargetType.Gamepad;
        public bool IsRightHandGripGamepad => _rightTargetType == HandTargetType.Gamepad;

        public Vector3 RightHandPosition => _rightHandTarget.position;
        public Vector3 LeftHandPosition => _leftHandTarget.position;


        [Inject]
        public void Initialize(
            IVRMLoadable vrmLoadable, 
            IKTargetTransforms ikTargets, 
            ParticleStore particleStore,
            GamepadProvider gamepadProvider,
            MidiControllerProvider midiControllerProvider
            )
        {
            _rightHandTarget = ikTargets.RightHand;
            _leftHandTarget = ikTargets.LeftHand;
            _particleStore = particleStore;
            vrmLoadable.VrmLoaded += OnVrmLoaded;
            vrmLoadable.VrmDisposing += OnVrmDisposing;        
            
            MidiHand = new MidiHandIkGenerator(this, midiControllerProvider);
            GamepadHand = new GamepadHandIKGenerator(this, gamepadProvider, gamepadSetting);
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
            
            fingerController.StartPressKeyMotion(keyName, EnablePresentationMode);	
            
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
            
            //mouseMove.MoveMouse(mousePosition);
            presentation.MoveMouse(mousePosition);
            SetRightHandIk(EnablePresentationMode ? HandTargetType.Presentation : HandTargetType.Mouse);
            if (_rightTargetType == HandTargetType.Mouse)
            {
                _particleStore.RequestMouseMoveParticle(mouseMove.ReferenceTouchpadPosition);
            }
        }

        public void ClickMouse(string button)
        {
            if (!EnablePresentationMode && EnableHidArmMotion)
            {
                fingerController.StartClickMotion(button);
                SetRightHandIk(HandTargetType.Mouse);   
                if (_rightTargetType == HandTargetType.Mouse)
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
            gamepadFinger.ButtonDown(key);
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
            GamepadHand.Update();
            MidiHand.Update();
            
            if (imageBaseHand.HasRightHandUpdate)
            {
                imageBaseHand.HasRightHandUpdate = false;
                SetRightHandIk(HandTargetType.ImageBaseHand);
            }

            if (imageBaseHand.HasLeftHandUpdate)
            {
                imageBaseHand.HasLeftHandUpdate = false;
                SetLeftHandIk(HandTargetType.ImageBaseHand);
            }
        }
        
        #endregion
        
        /// <summary> 既定の秒数をかけて手のIKを無効化します。 </summary>
        public void DisableHandIk()
        {
        }

        /// <summary> 既定の秒数をかけて手のIKを有効化します。 </summary>
        public void EnableHandIk()
        {
        }

        #endregion

        private void Start()
        {
            _currentRightHand = Typing.RightHand;
            _currentLeftHand = Typing.LeftHand;
            _leftHandStateBlendCount = HandIkToggleDuration;
            _rightHandStateBlendCount = HandIkToggleDuration;

            GamepadHand.Start();
            MidiHand.Start();
        }
        
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            fingerController.Initialize(info.animator);
            presentation.Initialize(info.animator);
            
            //NOTE: 初期姿勢は「トラッキングできてない(はずの)画像ベースハンドトラッキングのやつ」にします。
            //棒立ちサポートをめちゃ適当にやっちゃえ！というのがモチベです
            imageBaseHand.HasRightHandUpdate = false;
            SetRightHandIk(HandTargetType.ImageBaseHand);

            imageBaseHand.HasLeftHandUpdate = false;
            SetLeftHandIk(HandTargetType.ImageBaseHand);
        }

        private void OnVrmDisposing()
        {
            fingerController.Dispose();
            presentation.Dispose();
        }
        
        private void Update()
        {
            ExecuteOrCheckHandUpdates();
            
            //ねらい: 前のステートと今のステートをブレンドしながら実際にIKターゲットの位置、姿勢を更新する
            UpdateLeftHand();
            UpdateRightHand();
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

            _leftHandIkChangeCoolDown = HandIkTypeChangeCoolDown;

            var prevType = _leftTargetType;
            _leftTargetType = targetType;

            var ik =
                (targetType == HandTargetType.Keyboard) ? Typing.LeftHand :
                (targetType == HandTargetType.Gamepad) ? GamepadHand.LeftHand :
                (targetType == HandTargetType.MidiController) ? MidiHand.LeftHand : 
                (targetType == HandTargetType.ImageBaseHand) ? imageBaseHand.LeftHand :
                Typing.LeftHand;

            _prevLeftHand = _currentLeftHand;
            _currentLeftHand = ik;
            _leftHandStateBlendCount = 0f;

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
                imageBaseHand.InitializeHandPosture(ReactedHand.Left, _prevLeftHand);
            }
        }

        private void SetRightHandIk(HandTargetType targetType)
        {
            if (_rightTargetType == targetType)
            {
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
                (targetType == HandTargetType.ImageBaseHand) ? imageBaseHand.RightHand :
                Typing.RightHand;

            _prevRightHand = _currentRightHand;
            _currentRightHand = ik;
            _rightHandStateBlendCount = 0f;

            fingerController.RightHandPresentationMode = (targetType == HandTargetType.Presentation);

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
                imageBaseHand.InitializeHandPosture(ReactedHand.Right, _prevRightHand);
            }
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
        }

    }
}