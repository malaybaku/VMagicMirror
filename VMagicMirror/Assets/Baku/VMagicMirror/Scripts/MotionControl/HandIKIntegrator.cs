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

        [SerializeField] private Transform rightHandTarget = null;
        [SerializeField] private Transform leftHandTarget = null;

        [SerializeField] private TypingHandIKGenerator typing = null;
        public TypingHandIKGenerator Typing => typing;

        [SerializeField] private SmallGamepadHandIKGenerator smallGamepadHand = null;
        public SmallGamepadHandIKGenerator SmallGamepadHand => smallGamepadHand;
        

        [SerializeField] private GamepadFingerController gamepadFinger = null;

        [SerializeField] private MouseMoveHandIKGenerator mouseMove = null;
        public MouseMoveHandIKGenerator MouseMove => mouseMove;

        [SerializeField] private PresentationHandIKGenerator presentation = null;
        public PresentationHandIKGenerator Presentation => presentation;

        [SerializeField] private FingerController fingerController = null;

        [SerializeField] private ParticleStore particleStore = null;

        private float _leftHandStateBlendCount = 0f;
        private float _rightHandStateBlendCount = 0f;

        public bool EnableHidArmMotion { get; set; } = true;

        public bool UseGamepadForWordToMotion { get; set; } = false;
        
        //NOTE: このフラグではキーボードのみならずマウス入力も無視することに注意
        public bool UseKeyboardForWordToMotion { get; set; } = false;
        
        public bool EnablePresentationMode { get; set; }

        public bool IsLeftHandGripGamepad => _leftTargetType == HandTargetType.Gamepad;
        public bool IsRightHandGripGamepad => _rightTargetType == HandTargetType.Gamepad;

        [Inject] private IVRMLoadable _vrmLoadable = null;

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
            if (UseKeyboardForWordToMotion)
            {
                return;
            }
            
            var (hand, pos) = typing.PressKey(keyName, EnablePresentationMode);
            if (hand == ReactedHand.Left)
            {
                SetLeftHandIk(HandTargetType.Keyboard);
            }
            else if (hand == ReactedHand.Right)
            {
                SetRightHandIk(HandTargetType.Keyboard);
            }

            if (EnableHidArmMotion)
            {
                fingerController.StartPressKeyMotion(keyName, EnablePresentationMode);
            }

            if (hand != ReactedHand.None && EnableHidArmMotion)
            {
                particleStore.RequestParticleStart(pos);
            }
        }

        public void MoveMouse(Vector3 mousePosition)
        {
            if (UseKeyboardForWordToMotion)
            {
                return;
            }
            
            mouseMove.MoveMouse(mousePosition);
            presentation.MoveMouse(mousePosition);
            SetRightHandIk(EnablePresentationMode ? HandTargetType.Presentation : HandTargetType.Mouse);
        }

        public void ClickMouse(string button)
        {
            if (!EnablePresentationMode && EnableHidArmMotion && !UseKeyboardForWordToMotion)
            {
                fingerController.StartClickMotion(button);
                SetRightHandIk(HandTargetType.Mouse);   
            }
        }

        #endregion
        
        #region Gamepad
        
        //NOTE: 表情コントロール用にゲームパッドを使っている間は入力を無視する
        
        public void MoveLeftGamepadStick(Vector2 v)
        {
            if (UseGamepadForWordToMotion)
            {
                return;
            }
            smallGamepadHand.LeftStick(v);
            gamepadFinger.LeftStick(v);
            SetLeftHandIk(HandTargetType.Gamepad);
        }

        public void MoveRightGamepadStick(Vector2 v)
        {
            if (UseGamepadForWordToMotion)
            {
                return;
            }
            smallGamepadHand.RightStick(v);
            gamepadFinger.RightStick(v);
            SetRightHandIk(HandTargetType.Gamepad);
        }

        public void GamepadButtonDown(GamepadKey key)
        {
            smallGamepadHand.ButtonDown(key);

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
            smallGamepadHand.ButtonUp(key);

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
            smallGamepadHand.ButtonStick(pos);
            SetLeftHandIk(HandTargetType.Gamepad);
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

            _vrmLoadable.VrmLoaded += OnVrmLoaded;
            _vrmLoadable.VrmDisposing += OnVrmDisposing;
        }
        
        private void OnVrmLoaded(VrmLoadedInfo info)
        {
            fingerController.Initialize(info.animator);
            presentation.Initialize(info.animator);

            //ホームポジションを押させてIK位置を整える
            PressKey("F");
            PressKey("J");
        }

        private void OnVrmDisposing()
        {
            fingerController.Dispose();
            presentation.Dispose();
        }
        
        private void Update()
        {
            //ねらい: 前のステートと今のステートをブレンドしながら実際にIKターゲットの位置、姿勢を更新する
            UpdateLeftHand();
            UpdateRightHand();
        }

        //TODO: IKオン/オフとの兼ね合いがアレなのでどうにかしてね。

        private void UpdateLeftHand()
        {
            //普通の状態: 複数ステートのブレンドはせず、今のモードをそのまま通す
            if (_leftHandStateBlendCount >= HandIkToggleDuration)
            {
                leftHandTarget.localPosition = _currentLeftHand.Position;
                leftHandTarget.localRotation = _currentLeftHand.Rotation;
                return;
            }

            //NOTE: ここの下に来る時点では必ず_prevLeftHandに非null値が入る実装になってます

            _leftHandStateBlendCount += Time.deltaTime;
            //prevStateと混ぜるための比率
            float t = CubicEase(_leftHandStateBlendCount / HandIkToggleDuration);
            leftHandTarget.localPosition = Vector3.Lerp(
                _prevLeftHand.Position,
                _currentLeftHand.Position,
                t
            );

            leftHandTarget.localRotation = Quaternion.Slerp(
                _prevLeftHand.Rotation,
                _currentLeftHand.Rotation,
                t
            );
        }

        private void UpdateRightHand()
        {
            //普通の状態: 複数ステートのブレンドはせず、今のモードをそのまま通す
            if (_rightHandStateBlendCount >= HandIkToggleDuration)
            {
                rightHandTarget.localPosition = _currentRightHand.Position;
                rightHandTarget.localRotation = _currentRightHand.Rotation;
                return;
            }

            //NOTE: 実装上ここの下に来る時点で_prevRightHandが必ず非nullなのでnullチェックはすっ飛ばす
            
            _rightHandStateBlendCount += Time.deltaTime;
            //prevStateと混ぜるための比率
            float t = CubicEase(_rightHandStateBlendCount / HandIkToggleDuration);
            
            rightHandTarget.localPosition = Vector3.Lerp(
                _prevRightHand.Position,
                _currentRightHand.Position,
                t
            );

            rightHandTarget.localRotation = Quaternion.Slerp(
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

            var prevType = _leftTargetType;
            _leftTargetType = targetType;

            var ik =
                (targetType == HandTargetType.Keyboard) ? Typing.LeftHand :
                (targetType == HandTargetType.Gamepad) ? SmallGamepadHand.LeftHand :
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

        }

        private void SetRightHandIk(HandTargetType targetType)
        {
            if (_rightTargetType == targetType)
            {
                return;
            }

            var prevType = _rightTargetType;
            _rightTargetType = targetType;

            var ik =
                (targetType == HandTargetType.Mouse) ? MouseMove.RightHand :
                (targetType == HandTargetType.Keyboard) ? Typing.RightHand :
                (targetType == HandTargetType.Gamepad) ? SmallGamepadHand.RightHand :
                (targetType == HandTargetType.Presentation) ? Presentation.RightHand :
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
        }

    }
}