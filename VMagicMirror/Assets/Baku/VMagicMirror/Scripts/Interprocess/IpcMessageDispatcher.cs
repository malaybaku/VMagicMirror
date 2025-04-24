using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Baku.VMagicMirror.InterProcess
{
    public class IpcMessageDispatcher : IMessageReceiver, IMessageDispatcher
    {
        // NOTE: VmmCommandsの実体が整数値であり数が有限であることを踏まえて、配列で管理してしまう。
        // Queryのほうがスカスカで、Commandはだいたい埋まる…という想定
        // NOTE: QueryとCommandsが同一のVmmCommandsから採番されてるのをやめて、双方の配列がほぼ埋まるようにしてもOK
        private readonly Action<ReceivedCommand>[] _commandHandlers;
        private readonly Action<ReceivedQuery>[] _queryHandlers;
        
        private readonly ConcurrentQueue<ReceivedCommand> _receivedCommands = new();
        private readonly ConcurrentQueue<QueryQueueItem> _receivedQueries = new();

        public IpcMessageDispatcher()
        {
            _commandHandlers = new Action<ReceivedCommand>[(int)VmmCommands.LastCommandId];
            for (var i = 0; i < _commandHandlers.Length; i++)
            {
                _commandHandlers[i] = null!;
            }
            
            _queryHandlers = new Action<ReceivedQuery>[(int)VmmCommands.LastCommandId];
            for (var i = 0; i < _queryHandlers.Length; i++)
            {
                _queryHandlers[i] = null!;
            }
        }
        
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
        
        public void AssignCommandHandler(VmmCommands command, Action<ReceivedCommand> handler)
        {
            //NOTE: 同じコマンドを複数のハンドラが読む場合、Actionの加算をする(差し替えない)。クエリのほうも同様
            var index = (int)command;
            if (_commandHandlers[index] == null)
            {
                _commandHandlers[index] = handler;
            }
            else
            {
                _commandHandlers[index] += handler;
            }
        }

        public void AssignQueryHandler(VmmCommands query, Action<ReceivedQuery> handler)
        {
            var index = (int)query;
            if (_queryHandlers[index] == null)
            {
                _queryHandlers[index] = handler;
            }
            else
            {
                _queryHandlers[index] += handler;
            }
        }
        
        public void ReceiveCommand(ReceivedCommand command)
        {
            if (command.Command == VmmCommands.CommandArray)
            {
                //コマンドの一括送信を受け取ったとき: バラバラにしてキューに詰めておく
                var commands = CommandArrayParser.ParseCommandArray(command.GetStringValue());
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
            => _commandHandlers[(int)command.Command]?.Invoke(command);

        private void ProcessQuery(QueryQueueItem item)
        {
            _queryHandlers[(int)item.Query.Command]?.Invoke(item.Query);
            item.ResultSource.SetResult(item.Query.Result);
        }

        private class QueryQueueItem
        {
            public QueryQueueItem(ReceivedQuery query)
            {
                Query = query;
            }
            public ReceivedQuery Query { get; }
            public TaskCompletionSource<string> ResultSource { get; } = new();
        }
        
    }
}
