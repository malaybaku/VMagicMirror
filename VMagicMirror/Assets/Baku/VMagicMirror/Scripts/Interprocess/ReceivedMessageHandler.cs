using System;
using System.Collections.Concurrent;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    /// <summary> メッセージを受け取ってUIスレッドで再配布する </summary>
    public class ReceivedMessageHandler : MonoBehaviour
    {
        private readonly Subject<ReceivedCommand> _commandsSubject = new Subject<ReceivedCommand>();
        public IObservable<ReceivedCommand> Commands => _commandsSubject;

        //NOTE: 初期版では、購読側はクエリを即時処理するよう義務付ける。分かりやすいので。
        public event Action<ReceivedQuery> QueryRequested;

        private readonly ConcurrentQueue<ReceivedCommand> _receivedCommands = new ConcurrentQueue<ReceivedCommand>();
        private readonly ConcurrentQueue<QueryQueueItem> _receivedQueries = new ConcurrentQueue<QueryQueueItem>();

        private void Update()
        {
            while (_receivedCommands.TryDequeue(out var command))
            {
                ProcessCommand(command);
            }

            while(_receivedQueries.TryDequeue(out var query))
            {
                ProcessQuery(query);
            }
        }

        public void ReceiveCommand(ReceivedCommand command)
        {
            _receivedCommands.Enqueue(command);
        }

        public IObservable<string> ReceiveQuery(ReceivedQuery query)
        {
            var item = new QueryQueueItem(query);
            _receivedQueries.Enqueue(item);
            return item.ResultSubject;
        }

        private void ProcessCommand(ReceivedCommand command)
        {
            _commandsSubject.OnNext(command);
        }

        private void ProcessQuery(QueryQueueItem item)
        {
            QueryRequested?.Invoke(item.Query);
            item.ResultSubject.OnNext(item.Query.Result);
            item.ResultSubject.OnCompleted();
        }

        class QueryQueueItem
        {
            public QueryQueueItem(ReceivedQuery query)
            {
                Query = query;
                ResultSubject = new AsyncSubject<string>();
            }
            public ReceivedQuery Query { get; }
            public AsyncSubject<string> ResultSubject { get; }
        }

    }
}

