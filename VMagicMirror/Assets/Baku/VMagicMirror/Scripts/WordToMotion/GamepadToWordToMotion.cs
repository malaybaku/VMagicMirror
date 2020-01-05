using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Zenject;

namespace Baku.VMagicMirror
{
    /// <summary>ゲームパッド入力を受け取ってWord To Motionの実行リクエストに変換するやつ</summary>
    public class GamepadToWordToMotion : MonoBehaviour
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
        
        [Tooltip("ボタン押下イベントが押されたらしばらくイベント送出をストップするクールダウンタイム")]
        [SerializeField] private float cooldownTime = 0.3f;

        [Inject] private StatefulXinputGamePad _gamepadInput = null;

        /// <summary>Word to Motionの要素を実行してほしいとき、アイテムのインデックスを引数にして発火する。</summary>
        public event Action<int> RequestExecuteWordToMotionItem;
        
        public bool UseGamepadInput { get; set; } = false;
        
        private float _cooldownCount = 0;

        private void Start()
        {
            _gamepadInput.ButtonUpDown.Subscribe(data =>
            {
                if (UseGamepadInput &&
                    data.IsPressed &&
                    _cooldownCount <= 0 &&
                    _gamePadKeyToItemIndex.ContainsKey(data.Key))
                {
                    RequestExecuteWordToMotionItem?.Invoke(
                        _gamePadKeyToItemIndex[data.Key]
                        );
                    _cooldownCount = cooldownTime;
                }
            });

        }

        private void Update()
        {
            if (_cooldownCount > 0)
            {
                _cooldownCount -= Time.deltaTime;
            }
        }
    } 
}
