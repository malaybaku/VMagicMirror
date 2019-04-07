using System.Threading.Tasks;
using UniRx;
using Grpc.Core;

namespace Baku.VMagicMirror
{
    public class GrpcServerImpl : VmmGrpc.VmmGrpcBase
    {
        public ReceivedMessageHandler Handler { get; set; }

        public override Task<GenericCommandResponse> CommandGeneric(GenericCommandRequest request, ServerCallContext context)
        {
            Handler?.ReceiveCommand(new ReceivedCommand(request.Command, request.Args));
            return Task.FromResult(new GenericCommandResponse()
            {
                Result = true,
            });
        }

        public override async Task<GenericQueryResponse> QueryGeneric(GenericQueryRequest request, ServerCallContext context)
        {
            string result = "";
            if (Handler != null)
            {
                result = await Handler.ReceiveQuery(new ReceivedQuery(request.Command, request.Args));
            }

            return new GenericQueryResponse()
            {
                Result = result,
            };
        }
    }
}
