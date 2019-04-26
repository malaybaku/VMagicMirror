using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Collections.Concurrent;

namespace Baku.VMagicMirror
{
    public class ReceivedMessageHandler : MonoBehaviour
    {
        private readonly Subject<ReceivedCommand> _commandsSubject = new Subject<ReceivedCommand>();
        public IObservable<ReceivedCommand> Commands => _commandsSubject;

        //NOTE: 初期版では、購読側はクエリを即時処理するよう義務付ける。分かりやすいので。
        public event EventHandler<QueryEventArgs> QueryRequested;

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

        public void ReceiveCommand(string message)
        {
            string command = message.Split(':')[0];
            string content = message.Substring(command.Length + 1);
            ReceiveCommand(new ReceivedCommand(command, content));
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
            QueryRequested?.Invoke(this, new QueryEventArgs(item.Query));
            item.ResultSubject.OnNext(item.Query.Result);
            item.ResultSubject.OnCompleted();
        }

        public class QueryEventArgs : EventArgs
        {
            public QueryEventArgs(ReceivedQuery receivedQuery)
            {
                Query = receivedQuery;
            }
            public ReceivedQuery Query { get; }
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

