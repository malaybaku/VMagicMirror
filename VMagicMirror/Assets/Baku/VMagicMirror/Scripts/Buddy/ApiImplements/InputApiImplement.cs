using System;
using R3;
using UnityEngine;
using Zenject;

namespace Baku.VMagicMirror.Buddy
{
    /// <summary>
    /// 以下が取得できるクラス。
    /// - マウス位置をスクリーン座標系に無次元化した値
    /// - ゲームパッドの入力状態の全般
    ///   - 現在値と、ボタンについては押下/押上のイベント相当の情報
    /// </summary>
    /// <remarks>
    /// 正確なマウス位置、アバターのモーションを伴わないマウスクリック、キーボードのキーは取れない
    /// (ちょっとセンシティブな感じがするので)
    /// </remarks>
    public class InputApiImplement : PresenterBase
    {
        private readonly BuddySettingsRepository _buddySettingsRepository;
        private readonly IKeyMouseEventSource _keyMouseEventSource;
        private readonly XInputGamePad _gamepad;
        private readonly MousePositionProvider _mousePositionProvider;

        [Inject]
        public InputApiImplement(
            BuddySettingsRepository buddySettingsRepository,
            IKeyMouseEventSource keyMouseEventSource,
            XInputGamePad gamepad,
            MousePositionProvider mousePositionProvider)
        {
            _buddySettingsRepository = buddySettingsRepository;
            _keyMouseEventSource = keyMouseEventSource;
            _gamepad = gamepad;
            _mousePositionProvider = mousePositionProvider;
        }
        
        //TODO: Gamepad側がstickPositionをReactivePropertyとして公開するようになったらPresenterBaseの実装は無くしてOK
        public override void Initialize()
        {
            _keyMouseEventSource.KeyDown
                .Where(_ => InteractionApiEnabled)
                .Subscribe(key =>
                {
                    var keyName = key.ToLower() == "enter" ? "Enter" : "";
                    _onKeyboardKeyDown.OnNext(keyName);
                })
                .AddTo(this);
            _keyMouseEventSource.KeyUp
                .Where(_ => InteractionApiEnabled)
                .Subscribe(key =>
                {
                    var keyName = key.ToLower() == "enter" ? "Enter" : "";
                    _onKeyboardKeyUp.OnNext(keyName);
                })
                .AddTo(this);
                
            _gamepad.LeftStickPosition
                .Subscribe(p => _leftStickPosition = new Vector2(p.x / 32768f, p.y / 32768f))
                .AddTo(this);

            _gamepad.RightStickPosition
                .Subscribe(p => _rightStickPosition = new Vector2(p.x / 32768f, p.y / 32768f))
                .AddTo(this);
        }

        private bool InteractionApiEnabled => _buddySettingsRepository.InteractionApiEnabled.CurrentValue;

        /// <summary>
        /// 画面サイズを基準とし、マウスの現在位置をXYいずれも[-0.5, 0.5]くらいに収まる値として表現した値を取得する。
        /// アバターの表示ウィンドウにマウスが収まっていない場合、上記より大きな値を取ることがある
        /// </summary>
        /// <returns></returns>
        public Vector2 GetNonDimensionalMousePosition() 
            => _mousePositionProvider.RawNormalizedPositionNotClamped;

        private readonly Subject<string> _onKeyboardKeyDown = new();
        public Observable<string> OnKeyboardKeyDown => _onKeyboardKeyDown;
        
        private readonly Subject<string> _onKeyboardKeyUp = new();
        public Observable<string> OnKeyboardKeyUp => _onKeyboardKeyUp;

        // NOTE: 呼び出し元でGamepadKeyとintないしstringの変換をするのが期待値
        // スクリプト上で `GamepadButton.A` みたく書かせて実態がintになってるのが無難そうではある
        public bool GetGamepadButton(GamepadKey key)
        {
            if (!InteractionApiEnabled)
            {
                return false;
            }
            
            return _gamepad.GetButtonDown(key);
        }

        //TODO: この辺のキャッシュはGamepad側のリファクタで不要になるはず
        private Vector2 _leftStickPosition = Vector2.zero;
        private Vector2 _rightStickPosition = Vector2.zero;
        public Vector2 GetGamepadLeftStickPosition()
        {
            if (!InteractionApiEnabled)
            {
                return Vector2.zero;
            }

            return _leftStickPosition;
        }

        public Vector2 GetGamepadRightStickPosition()
        {
            if (!InteractionApiEnabled)
            {
                return Vector2.zero;
            }
            
            return _rightStickPosition;
        }

        public Observable<GamepadKey> GamepadButtonDown => _gamepad
            .ButtonUpDown
            .Where(_ => InteractionApiEnabled)
            .Where(data => data.IsPressed)
            .Select(data => data.Key);
        public Observable<GamepadKey> GamepadButtonUp => _gamepad
            .ButtonUpDown
            .Where(_ => InteractionApiEnabled)
            .Where(data => !data.IsPressed)
            .Select(data => data.Key);
        
    }
}
