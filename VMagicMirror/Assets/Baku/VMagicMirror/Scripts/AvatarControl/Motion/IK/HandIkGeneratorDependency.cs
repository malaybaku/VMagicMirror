using System;
using UniRx;
using UnityEngine;

namespace Baku.VMagicMirror.IK
{
    //HandIkGeneratorから参照できる様々なアレです
    public class HandIkGeneratorDependency
    {
        public HandIkGeneratorDependency(
            MonoBehaviour component, 
            HandIkReactionSources reactions,
            HandIkRuntimeConfigs runtimeConfig,
            HandIkInputEvents inputEvents,
            IHandIkGetter handIkGetter
            )
        {
            Component = component;
            Reactions = reactions;
            Config = runtimeConfig;
            Events = inputEvents;
            HandIkGetter = handIkGetter;
        }

        /// <summary>
        /// NOTE: コルーチンを引っ掛けたりUniRxのAddToを呼んだりするのに使って良い
        /// </summary>
        public MonoBehaviour Component { get; }
        public HandIkReactionSources Reactions { get; }
        public HandIkRuntimeConfigs Config { get; }
        public HandIkInputEvents Events { get; }
        public IHandIkGetter HandIkGetter { get; }
    }
    
    /// <summary> 手のIK計算をしたついでで操作することがある諸々をまとめたクラス </summary>
    public class HandIkReactionSources
    {
        public HandIkReactionSources(
            ParticleStore particleStore, 
            FingerController fingerController,
            GamepadFingerController gamepadFinger, 
            ArcadeStickFingerController arcadeStickFinger
            )
        {
            ParticleStore = particleStore;
            FingerController = fingerController;
            GamepadFinger = gamepadFinger;
            ArcadeStickFinger = arcadeStickFinger;
        }
        
        public ParticleStore ParticleStore { get; }
        public FingerController FingerController { get; }
        public GamepadFingerController GamepadFinger { get; }
        public ArcadeStickFingerController ArcadeStickFinger { get; }
    }
    
    /// <summary> 手のIK計算において実行時にガチャガチャ変化するコンフィグをまとめたクラス </summary>
    public class HandIkRuntimeConfigs
    {
        public HandIkRuntimeConfigs(
            ReactiveProperty<bool> isAlwaysHandDown, 
            ReactiveProperty<HandTargetType> leftTarget,
            ReactiveProperty<HandTargetType> rightTarget,
            ReactiveProperty<KeyboardAndMouseMotionModes> keyMouseMode,
            ReactiveProperty<GamepadMotionModes> gamepadMode,
            ReactiveProperty<WordToMotionDeviceAssign> wordToMotionDevice,
            Func<ReactedHand, HandTargetType, bool> checkCooldownFunc,
            Func<bool> checkKeyboardAndMouseHandsCanMoveDown
            )
        {     
            IsAlwaysHandDown = isAlwaysHandDown;
            LeftTarget = leftTarget;
            RightTarget = rightTarget;
            KeyboardAndMouseMotionMode = keyMouseMode;
            GamepadMotionMode = gamepadMode;
            WordToMotionDevice = wordToMotionDevice;
            _checkCooldownFunc = checkCooldownFunc;
            _checkKeyboardAndMouseHandsCanMoveDown = checkKeyboardAndMouseHandsCanMoveDown;
        }

        private readonly Func<ReactedHand, HandTargetType, bool> _checkCooldownFunc;
        private readonly Func<bool> _checkKeyboardAndMouseHandsCanMoveDown;

        public IReadOnlyReactiveProperty<HandTargetType> LeftTarget { get; }
        public IReadOnlyReactiveProperty<HandTargetType> RightTarget { get; }
        public IReadOnlyReactiveProperty<bool> IsAlwaysHandDown { get; }
        public IReadOnlyReactiveProperty<KeyboardAndMouseMotionModes> KeyboardAndMouseMotionMode { get; }
        public IReadOnlyReactiveProperty<GamepadMotionModes> GamepadMotionMode { get; }
        public IReadOnlyReactiveProperty<WordToMotionDeviceAssign> WordToMotionDevice { get; }

        //NOTE: この辺は値というよりメソッドライクなものなので、RP<T>にせずにgetter methodを使います
        public bool CheckCoolDown(ReactedHand hand, HandTargetType targetType) 
            => _checkCooldownFunc(hand, targetType);
        public bool CheckKeyboardAndMouseHandsCanMoveDown() => _checkKeyboardAndMouseHandsCanMoveDown();
    }

    /// <summary> 手のIKを更新するきっかけになるような、キーやマウスの入力イベントをまとめたクラス </summary>
    /// <remarks>
    /// ここは若干甘い設計だが、eventとイベント発火コードを同居させることで話を簡単にしてます
    /// </remarks>
    public class HandIkInputEvents
    {
        // Keyboard and Mouse

        public event Action<string> KeyDown;
        public event Action<string> KeyUp;
        public event Action<Vector3> MoveMouse;
        public event Action<string> OnMouseButton;
        
        public void RaiseKeyDown(string keyName) => KeyDown?.Invoke(keyName);
        public void RaiseKeyUp(string keyName) => KeyUp?.Invoke(keyName);

        public void RaiseMoveMouse(Vector3 mousePosition) => MoveMouse?.Invoke(mousePosition);

        public void RaiseOnMouseButton(string button) => OnMouseButton?.Invoke(button);

        // Gamepad
        
        public event Action<Vector2> MoveLeftGamepadStick;
        public event Action<Vector2> MoveRightGamepadStick;
        public event Action<GamepadKey> GamepadButtonDown;
        public event Action<GamepadKey> GamepadButtonUp;
        public event Action<Vector2Int> GamepadButtonStick;

        public void RaiseMoveLeftGamepadStick(Vector2 v) => MoveLeftGamepadStick?.Invoke(v);
        public void RaiseMoveRightGamepadStick(Vector2 v) => MoveRightGamepadStick?.Invoke(v);
        public void RaiseGamepadButtonStick(Vector2Int v) => GamepadButtonStick?.Invoke(v);
        public void RaiseGamepadButtonDown(GamepadKey key) => GamepadButtonDown?.Invoke(key);
        public void RaiseGamepadButtonUp(GamepadKey key) => GamepadButtonUp?.Invoke(key);

        // MIDI
        
        public event Action<int> NoteOn;
        public event Action<int, float> KnobValueChange;

        public void RaiseNoteOn(int noteNumber) => NoteOn?.Invoke(noteNumber);
        public void RaiseKnobValueChange(int knobNumber, float value) => KnobValueChange?.Invoke(knobNumber, value);
    }

    //IK位置を明示的に取得したいときに見に行くクラス。
    //State遷移時に直前ステートを見に行くと用が足りない場合だけ使うもので、使うケースは少ない
    public interface IHandIkGetter
    {
        public IIKData GetLeft();
        public IIKData GetRight();
    }
}
