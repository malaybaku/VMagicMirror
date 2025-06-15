using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Baku.VMagicMirror.InterProcess
{
    public class IpcMessageDispatcher : IMessageReceiver, IMessageDispatcher
    {
        // NOTE: 配列 vs. Dictの2通りがあるが、どっちも配列だと歯抜けになるのが気になるため、Dictにしている
        private readonly Dictionary<ushort, Action<ReceivedCommand>> _commandHandlers = new();
        private readonly Dictionary<ushort, Action<ReceivedQuery>> _queryHandlers=  new();
        
        private readonly ConcurrentQueue<ReceivedCommand> _receivedCommands = new();
        private readonly ConcurrentQueue<QueryQueueItem> _receivedQueries = new();
        
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
            var index = (ushort)command;
            if (!_commandHandlers.TryAdd(index, handler))
            {
                _commandHandlers[index] += handler;
            }
        }

        public void AssignQueryHandler(VmmCommands query, Action<ReceivedQuery> handler)
        {
            var index = (ushort)query;
            if (!_queryHandlers.TryAdd(index, handler))
            {
                _queryHandlers[index] += handler;
            }
        }
        
        public void ReceiveCommand(ReceivedCommand command)
        {
            _receivedCommands.Enqueue(command);
        }

        public Task<string> ReceiveQuery(ReceivedQuery query)
        {
            var item = new QueryQueueItem(query);
            _receivedQueries.Enqueue(item);
            return item.ResultSource.Task;
        }

        private void ProcessCommand(ReceivedCommand command)
        {
            if (_commandHandlers.TryGetValue((ushort)command.Command, out var handler))
            {
                handler(command);
            }
        }

        private void ProcessQuery(QueryQueueItem item)
        {
            if (_queryHandlers.TryGetValue((ushort)item.Query.Command, out var handler))
            {
                handler(item.Query);
                item.ResultSource.SetResult(item.Query.Result);
            }
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
