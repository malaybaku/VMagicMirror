using System;
using System.Collections.Generic;
using UniRx;

namespace Baku.VMagicMirror.WordToMotion
{
    public class GamePadRequestSource : PresenterBase, IRequestSource
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
            [GamepadKey.RShoulder] = 9,
            [GamepadKey.LShoulder] = 10,
            [GamepadKey.Select] = 11,
        };

        private readonly XInputGamePad _gamePad;

        public SourceType SourceType => SourceType.Gamepad;
        private readonly Subject<int> _runMotionRequested = new Subject<int>();
        public IObservable<int> RunMotionRequested => _runMotionRequested;

        public void SetActive(bool active)
        {
            //何もしない: 非activeになったからといってゲームパッド読み取りを止めたりはしない
        }

        public GamePadRequestSource(XInputGamePad gamePad)
        {
            _gamePad = gamePad;
        }
        
        public override void Initialize()
        {
            _gamePad.ButtonUpDown
                .Subscribe(data =>
                {
                    if (data.IsPressed && _gamePadKeyToItemIndex.ContainsKey(data.Key))
                    {
                        _runMotionRequested.OnNext(_gamePadKeyToItemIndex[data.Key]);
                    }
                })
                .AddTo(this);
        }
    }
}
