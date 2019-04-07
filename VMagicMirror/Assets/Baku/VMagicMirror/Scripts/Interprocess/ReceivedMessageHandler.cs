using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Baku.VMagicMirror
{
    public class ReceivedMessageHandler : MonoBehaviour
    {
        private readonly Subject<ReceivedCommand> _commandsSubject = new Subject<ReceivedCommand>();
        public IObservable<ReceivedCommand> Commands => _commandsSubject;

        //NOTE: 初期版では、購読側はクエリを即時処理するよう義務付ける。分かりやすいので。
        public event EventHandler<QueryEventArgs> QueryRequested;

        private readonly object _receivedCommandsLock = new object();
        private Queue<ReceivedCommand> _receivedCommands = new Queue<ReceivedCommand>();
        private Queue<ReceivedCommand> ReceivedCommands
        {
            get { lock (_receivedCommandsLock) return _receivedCommands; }
        }

        private readonly object _receivedQueriesLock = new object();
        private readonly Queue<QueryQueueItem> _receivedQueries = new Queue<QueryQueueItem>();
        private Queue<QueryQueueItem> ReceivedQueries
        {
            get { lock (_receivedQueriesLock) return _receivedQueries; }
        }

        private void Update()
        {
            while (ReceivedCommands.Count > 0)
            {
                ProcessCommand(ReceivedCommands.Dequeue());
            }

            while(ReceivedCommands.Count > 0)
            {
                ProcessQuery(ReceivedQueries.Dequeue());
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
            ReceivedCommands.Enqueue(command);
        }

        public IObservable<string> ReceiveQuery(string query)
        {
            string command = query.Split(':')[0];
            string content = query.Substring(command.Length + 1);
            return ReceiveQuery(new ReceivedQuery(command, content));
        }

        public IObservable<string> ReceiveQuery(ReceivedQuery query)
        {
            var item = new QueryQueueItem(query);
            ReceivedQueries.Enqueue(item);
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

