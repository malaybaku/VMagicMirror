using System;
using System.Threading;
using Baku.VMagicMirror.WordToMotion;
using Cysharp.Threading.Tasks;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary>
    /// 現在実行中のWordToMotionの名前(っぽい値)が取得できるすごいやつだよ
    /// </summary>
    public class WordToMotionEventApiImplement : PresenterBase
    {
        // 再生秒数がこれより短いWord to Motionを呼び出した場合、Stopを呼んだのと同等に扱う (たぶんリセット目的の処理なので)
        private const float IgnoreDurationSecond = 0.3f;

        private readonly WordToMotionEventBroker _eventBroker;

        public WordToMotionEventApiImplement(WordToMotionEventBroker eventBroker)
        {
            _eventBroker = eventBroker;
        }

        private CancellationTokenSource _cts;
        
        // NOTE: Durationつきでスクリプトに何か公開したい場合は別途検討が必要
        
        private readonly ReactiveProperty<string> _currentWordToMotionName = new("");
        // NOTE: このRP<string>は「だいたい正しい値」を返す…くらいの精度で実装している。
        // - WordToMotionRunnerとの連動は緩いので、モーションの出始めや終了の補間時間とかは考慮されない
        // - 「モーションがループしてるときに表情変更だけのWtMを呼び出した」みたいなケースで、モーション側のWtM名は上書きされてわからなくなる
        public IReadOnlyReactiveProperty<string> CurrentWordToMotionName => _currentWordToMotionName;
        
        //TODO: Start (Durationつき) + Stopped が分かるとCurrentWordToMotionNameみたいなプロパティが公開できるのでは？
        public override void Initialize()
        {
            // NOTE: (Word to Motionは実行優先度がめっちゃ高いので) リクエストが無視される可能性はない…という前提でこういう実装になっている
            _eventBroker.Started
                .Subscribe(value =>
                {
                    CancelTask();
                    _cts = new CancellationTokenSource();
                    OnStartedAsync(value.request, value.duration, _cts.Token).Forget();
                })
                .AddTo(this);
            _eventBroker.Stopped
                .Subscribe(_ => OnStoppedAsync())
                .AddTo(this);
        }

        public override void Dispose()
        {
            base.Dispose();
            CancelTask();
        }
        
        private async UniTaskVoid OnStartedAsync(
            MotionRequest request, float duration, CancellationToken cancellationToken)
        {
            if (duration > 0 && duration < IgnoreDurationSecond)
            {
                _currentWordToMotionName.Value = "";
                return;
            }

            _currentWordToMotionName.Value = request.Word;
            if (duration < 0)
            {
                // ループ相当の入力が来た → 追加でWtMを呼ばない限りは値をキープしとく
                return;
            }

            await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: cancellationToken);
            _currentWordToMotionName.Value = "";
        }

        private void OnStoppedAsync()
        {
            CancelTask();
            _currentWordToMotionName.Value = "";
        }
        
        private void CancelTask()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}
