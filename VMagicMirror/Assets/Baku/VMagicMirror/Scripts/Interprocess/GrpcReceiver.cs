using UnityEngine;
using Grpc.Core;

namespace Baku.VMagicMirror
{
    public class GrpcReceiver : MonoBehaviour
    {
        [SerializeField]
        private ReceivedMessageHandler handler = null;

        [SerializeField]
        string ipAddress = "127.0.0.1";

        //NOTE: ポート番号に深い意味はない
        [SerializeField]
        int port = 53241;

        [SerializeField]
        int receiveTimeoutMillisec = 2000;

        private Server _server;

        private void Start()
        {
            _server = new Server()
            {
                Services =
                {
                    VmmGrpc.BindService(new GrpcServerImpl() { Handler = handler })
                },
                Ports =
                {
                    new ServerPort(ipAddress, port, ServerCredentials.Insecure),
                },
            };
            _server.Start();
        }

        private void OnDestroy()
        {
            _server?.ShutdownAsync()?.Wait();
        }
    }
}

