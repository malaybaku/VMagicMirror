using System;
using System.Collections.Generic;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary>ゲームパッド入力を受け取ってWord To Motionの実行リクエストに変換するやつ</summary>
    public class GamepadToWordToMotion
    {
        //決め打ち設計された、ボタンと実行するアイテムのインデックスのマッピング。
        //このクラスでは配列外とかそういうのは考慮しないことに注意
        private static readonly Dictionary<GamepadKey, int> _gamePadKeyToItemIndex = new Dictionary<GamepadKey, int>()
        {
            [GamepadKey.Start] = 0,
            [GamepadKey.A] = 1,
            [GamepadKey.B] = 2,
            [GamepadKey.X] = 3,
            [GamepadKey.Y] = 4,
            [GamepadKey.UP] = 5,
            [GamepadKey.RIGHT] = 6,
            [GamepadKey.DOWN] = 7,
            [GamepadKey.LEFT] = 8,
        };

        public GamepadToWordToMotion(XInputGamePad gamepadInput)
        {
            _gamepadObserve = gamepadInput.ButtonUpDown.Subscribe(data =>
            {
                if (data.IsPressed && _gamePadKeyToItemIndex.ContainsKey(data.Key))
                {
                    RequestExecuteWordToMotionItem?.Invoke(_gamePadKeyToItemIndex[data.Key]);
                }
            });
        }

        private readonly IDisposable _gamepadObserve;

        /// <summary>Word to Motionの要素を実行してほしいとき、アイテムのインデックスを引数にして発火する。</summary>
        public event Action<int> RequestExecuteWordToMotionItem;
    } 
}
