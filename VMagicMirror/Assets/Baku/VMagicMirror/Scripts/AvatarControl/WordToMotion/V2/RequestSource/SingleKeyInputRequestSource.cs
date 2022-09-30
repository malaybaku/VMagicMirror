using System;
using System.Collections.Generic;
using UniRx;

namespace Baku.VMagicMirror.WordToMotion
{
    public class SingleKeyInputRequestSource : PresenterBase, IRequestSource
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
            ["D9"] = 9,
            ["NumPad0"] = 0,
            ["NumPad1"] = 1,
            ["NumPad2"] = 2,
            ["NumPad3"] = 3,
            ["NumPad4"] = 4,
            ["NumPad5"] = 5,
            ["NumPad6"] = 6,
            ["NumPad7"] = 7,
            ["NumPad8"] = 8,
            ["NumPad9"] = 9,
        };

        private readonly IKeyMouseEventSource _keyMouseEventSource;

        public SourceType SourceType => SourceType.KeyboardTenKey;
        private readonly Subject<int> _runMotionRequested = new Subject<int>();
        public IObservable<int> RunMotionRequested => _runMotionRequested;

        private bool _isActive = false;
        
        public void SetActive(bool active) => _isActive = active;

        public SingleKeyInputRequestSource(IKeyMouseEventSource keyMouseEventSource)
        {
            _keyMouseEventSource = keyMouseEventSource;
        }

        public override void Initialize()
        {
            _keyMouseEventSource.RawKeyDown
                .Subscribe(keyName =>
                {
                    //NOTE: D0-D8とNumPad系のキーはサニタイズ対象じゃないので、そのまま受け取っても大丈夫
                    if (_isActive && _keyToItemIndex.ContainsKey(keyName))
                    {
                        _runMotionRequested.OnNext(_keyToItemIndex[keyName]);
                    }
                })
                .AddTo(this);
        }
    }
}
