using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Baku.VMagicMirror.InterProcess
{
    public class IpcMessageDispatcher : IMessageReceiver, IMessageDispatcher
    {
        private readonly Dictionary<string, Action<ReceivedCommand>> _commandHandlers
            = new Dictionary<string, Action<ReceivedCommand>>();
        
        private readonly Dictionary<string, Action<ReceivedQuery>> _queryHandlers
            = new Dictionary<string, Action<ReceivedQuery>>();

        private readonly ConcurrentQueue<ReceivedCommand> _receivedCommands = new ConcurrentQueue<ReceivedCommand>();
        private readonly ConcurrentQueue<QueryQueueItem> _receivedQueries = new ConcurrentQueue<QueryQueueItem>();

        //NOTE: ITickableっぽいからTickにしてるだけで、メインスレッド保証があるならどこで呼んでもいい。
        public void Tick()
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
        
        public void AssignCommandHandler(string command, Action<ReceivedCommand> handler)
        {
            //NOTE: 同じコマンドを複数のハンドラが読む場合、Actionの加算になる。差し替えてはいけないのがポイント
            if (_commandHandlers.ContainsKey(command))
            {
                _commandHandlers[command] += handler;
            }
            else
            {
                _commandHandlers[command] = handler;
            }
        }

        public void AssignQueryHandler(string query, Action<ReceivedQuery> handler)
        {
            if (_queryHandlers.ContainsKey(query))
            {
                _queryHandlers[query] += handler;
            }
            else
            {
                _queryHandlers[query] = handler;
            }
        }
        
        public void ReceiveCommand(ReceivedCommand command)
        {
            if (command.Command == VmmCommands.CommandArray)
            {
                //コマンドの一括送信を受け取ったとき: バラバラにしてキューに詰めておく
                var commands = CommandArrayParser.ParseCommandArray(command.Content);
                foreach (var c in commands)
                {
                    _receivedCommands.Enqueue(c);
                }
            }
            else
            {
                //普通の受信
                _receivedCommands.Enqueue(command);
            }
        }

        public Task<string> ReceiveQuery(ReceivedQuery query)
        {
            var item = new QueryQueueItem(query);
            _receivedQueries.Enqueue(item);
            return item.ResultSource.Task;
        }

        private void ProcessCommand(ReceivedCommand command)
        {
            if (_commandHandlers.TryGetValue(command.Command, out var handler))
            {
                handler?.Invoke(command);
            }
        }

        private void ProcessQuery(QueryQueueItem item)
        {
            if (_queryHandlers.TryGetValue(item.Query.Command, out var handler))
            {
                handler?.Invoke(item.Query);
            }
            item.ResultSource.SetResult(item.Query.Result);
        }

        class QueryQueueItem
        {
            public QueryQueueItem(ReceivedQuery query)
            {
                Query = query;
            }
            public ReceivedQuery Query { get; }
            public TaskCompletionSource<string> ResultSource { get; } = new TaskCompletionSource<string>();
        }
        
    }
}
