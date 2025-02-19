using System;
using UniRx;
using UnityEngine;

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
        private readonly IKeyMouseEventSource _keyMouseEventSource;
        private readonly XInputGamePad _gamepad;
        private readonly MousePositionProvider _mousePositionProvider;

        public InputApiImplement(
            IKeyMouseEventSource keyMouseEventSource,
            XInputGamePad gamepad,
            MousePositionProvider mousePositionProvider)
        {
            _keyMouseEventSource = keyMouseEventSource;
            _gamepad = gamepad;
            _mousePositionProvider = mousePositionProvider;
        }

        //TODO: Gamepad側がstickPositionをReactivePropertyとして公開するようになったらPresenterBaseの実装は無くしてOK
        public override void Initialize()
        {

            _keyMouseEventSource.KeyDown
                .Subscribe(key =>
                {
                    var keyName = key.ToLower() == "enter" ? "enter" : "";
                    _onKeyboardKeyDown.OnNext(keyName);
                })
                .AddTo(this);
            _keyMouseEventSource.KeyUp
                .Subscribe(key =>
                {
                    var keyName = key.ToLower() == "enter" ? "enter" : "";
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

        /// <summary>
        /// 画面サイズを基準とし、マウスの現在位置をXYいずれも[-0.5, 0.5]くらいに収まる値として表現した値を取得する。
        /// アバターの表示ウィンドウにマウスが収まっていない場合、上記より大きな値を取ることがある
        /// </summary>
        /// <returns></returns>
        public Vector2 GetNonDimensionalMousePosition() 
            => _mousePositionProvider.RawNormalizedPositionNotClamped;

        private readonly Subject<string> _onKeyboardKeyDown = new();
        public IObservable<string> OnKeyboardKeyDown => _onKeyboardKeyDown;
        
        private readonly Subject<string> _onKeyboardKeyUp = new();
        public IObservable<string> OnKeyboardKeyUp => _onKeyboardKeyUp;

        // NOTE: 呼び出し元でGamepadKeyとintないしstringの変換をするのが期待値
        // スクリプト上で `GamepadButton.A` みたく書かせて実態がintになってるのが無難そうではある
        public bool GetGamepadButton(GamepadKey key) => _gamepad.GetButtonDown(key);

        //TODO: この辺のキャッシュはGamepad側のリファクタで不要になるはず
        private Vector2 _leftStickPosition =Vector2.zero;
        private Vector2 _rightStickPosition =Vector2.zero;
        public Vector2 GetGamepadLeftStickPosition() => _leftStickPosition;
        public Vector2 GetGamepadRightStickPosition() => _rightStickPosition;
        
        public IObservable<(GamepadKey, bool)> GamepadButton => _gamepad
            .ButtonUpDown
            .Select(data => (data.Key, data.IsPressed));
        
    }
}
