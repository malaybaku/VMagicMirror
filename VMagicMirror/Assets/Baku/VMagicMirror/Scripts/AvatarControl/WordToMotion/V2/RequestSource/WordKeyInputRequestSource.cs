using System;
using System.Linq;
using R3;

namespace Baku.VMagicMirror.WordToMotion
{
    public sealed class WordKeyInputRequestSource : PresenterBase, IRequestSource
    {
        //この秒数だけ入力がなければ単語入力が途切れたとみなす
        private const float KeyInputForgetTime = 1.0f;

        public SourceType SourceType => SourceType.KeyboardTyping;
        private readonly Subject<int> _runMotionRequested = new Subject<int>();
        public IObservable<int> RunMotionRequested => _runMotionRequested;

        private readonly WordToMotionRequestRepository _repository;
        private readonly IKeyMouseEventSource _keyMouseEventSource;
        private readonly WordAnalyzer _wordAnalyzer = new WordAnalyzer();
        private bool _isActive;
        
        public WordKeyInputRequestSource(
            WordToMotionRequestRepository repository,
            IKeyMouseEventSource keyMouseEventSource)
        {
            _repository = repository;
            _keyMouseEventSource = keyMouseEventSource;
        }
        
        public override void Initialize()
        {
            _repository.Requests
                .Subscribe(requests => _wordAnalyzer.LoadWordSet(
                    requests.Select(r => r.Word).ToArray()
                ))
                .AddTo(this);

            _keyMouseEventSource.RawKeyDown
                .Subscribe(keyName =>
                {
                    if (_isActive)
                    {
                        _wordAnalyzer.Add(KeyName2Char(keyName));
                    }
                })
                .AddTo(this);

            _keyMouseEventSource.RawKeyDown
                .Throttle(TimeSpan.FromSeconds(KeyInputForgetTime))
                .Subscribe(_ => _wordAnalyzer.Clear())
                .AddTo(this);

            _wordAnalyzer.WordDetectedAsObservable
                .Subscribe(word =>
                {
                    var index = _repository.FindIndex(word);
                    if (index >= 0)
                    {
                        _runMotionRequested.OnNext(index);
                    }
                })
                .AddTo(this);
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            if (!active)
            {
                _wordAnalyzer.Clear();
            }
        }
        
        private static char KeyName2Char(string keyName)
        {
            if (keyName.Length == 1)
            {
                //a-z
                return keyName.ToLower()[0];
            }
            else if (keyName.Length == 2 && keyName[0] == 'D' && char.IsDigit(keyName[1]))
            {
                //D0 ~ D9 (テンキーじゃないほうの0~9)
                return keyName[1];
            }
            else if (keyName.Length == 7 && keyName.StartsWith("NumPad") && char.IsDigit(keyName[6]))
            {
                //NumPad0 ~ NumPad9 (テンキーの0~9)
                return keyName[6];
            }
            else
            {
                //TEMP: 「ヘンな文字でワードが途切れた」という情報だけ残す
                return ' ';
            }
        }
    }
}