using System;
using System.Collections.Generic;
using UniRx;

namespace Baku.VMagicMirror
{
    public sealed class KeyboardToWordToMotion
    {
        //決め打ち設計された、ボタンと実行するアイテムのインデックスのマッピング。
        //このクラスでは配列外とかそういうのは考慮しないことに注意
        private static readonly Dictionary<string, int> _keyToItemIndex = new Dictionary<string, int>()
        {
            ["D0"] = 0,
            ["D1"] = 1,
            ["D2"] = 2,
            ["D3"] = 3,
            ["D4"] = 4,
            ["D5"] = 5,
            ["D6"] = 6,
            ["D7"] = 7,
            ["D8"] = 8,
            ["NumPad0"] = 0,
            ["NumPad1"] = 1,
            ["NumPad2"] = 2,
            ["NumPad3"] = 3,
            ["NumPad4"] = 4,
            ["NumPad5"] = 5,
            ["NumPad6"] = 6,
            ["NumPad7"] = 7,
            ["NumPad8"] = 8,
        };

        /// <summary>Word to Motionの要素を実行してほしいとき、アイテムのインデックスを引数にして発火する。</summary>
        public event Action<int> RequestExecuteWordToMotionItem;
        
        private IDisposable _inputObserver;

        public KeyboardToWordToMotion(RawInputChecker rawInputChecker)
        {
            _inputObserver = rawInputChecker.PressedRawKeys.Subscribe(keyName =>
            {
                //NOTE: D0-D8とNumPad系のキーはサニタイズ対象じゃないので、そのまま受け取っても大丈夫
                if (_keyToItemIndex.ContainsKey(keyName))
                {
                    RequestExecuteWordToMotionItem?.Invoke(_keyToItemIndex[keyName]);
                }
            });
        }
    }
}
