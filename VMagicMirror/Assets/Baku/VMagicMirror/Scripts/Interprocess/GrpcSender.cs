using System.Threading.Tasks;
using UnityEngine;
using Grpc.Core;
using System;

namespace Baku.VMagicMirror
{
    public class GrpcSender : MonoBehaviour
    {
        [SerializeField]
        private string targetIpAddress = "127.0.0.1";
        [SerializeField]
        private int targetPort = 53242;

        private Channel _channel;
        private VmmGrpc.VmmGrpcClient _client;

        private void Start()
        {
            _channel = new Channel(targetIpAddress + ":" + targetPort, ChannelCredentials.Insecure);
            _client = new VmmGrpc.VmmGrpcClient(_channel);
        }

        public void SendCommand(Message message)
        {
            try
            {
                _client.CommandGeneric(new GenericCommandRequest()
                {
                    Command = message.Command,
                    Args = message.Content,
                });
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public async Task<string> SendQueryAsync(Message message)
        {
            try
            {
                var response = await _client.QueryGenericAsync(new GenericQueryRequest()
                {
                    Command = message.Command,
                    Args = message.Content,
                });

                return response.Result;
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
                return "";
            }
        }
    }
}
