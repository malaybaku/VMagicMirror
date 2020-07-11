// using System;
// using System.Threading.Tasks;
// using UnityEngine;
// using UniRx;
// using Baku.VMagicMirror.Mmf;
//
// namespace Baku.VMagicMirror
// {
//     public class MmfServer : MonoBehaviour, IMessageSender
//     {
//         private const string ChannelName = "Baku.VMagicMirror";
//
//         [SerializeField] private ReceivedMessageHandler handler = null;
//         
//         private MemoryMappedFileConnectServer _server;
//         
//         private async void Start()
//         {
//             _server = new MemoryMappedFileConnectServer();
//             _server.ReceiveCommand += OnReceiveCommand;
//             _server.ReceiveQuery += OnReceiveQuery;
//             //NOTE: awaitに特に意味は無いことに注意！
//             await _server.Start(ChannelName);
//         }
//
//         private void OnDestroy()
//         {
//             _server.Stop();
//         }
//
//         public void SendCommand(Message message)
//         {
//             _server.SendCommand(message.Command + ":" + message.Content);
//         }
//
//         public async Task<string> SendQueryAsync(Message message)
//         {
//             return await _server.SendQueryAsync(message.Command + ":" + message.Content);
//         }
//
//         private void OnReceiveCommand(object sender, ReceiveCommandEventArgs e)
//         {
//             string rawContent = e.Command;
//             int i = FindColonCharIndex(rawContent);
//             string command = (i == -1) ? rawContent : rawContent.Substring(0, i);
//             string content = (i == -1) ? "" : rawContent.Substring(i + 1);
//
//             handler.ReceiveCommand(new ReceivedCommand(command, content));
//         }
//         
//         private async void OnReceiveQuery(object sender, ReceiveQueryEventArgs e)
//         {
//             string rawContent = e.Query.Query;
//             int i = FindColonCharIndex(rawContent);
//             string command = (i == -1) ? rawContent : rawContent.Substring(0, i);
//             string content = (i == -1) ? "" : rawContent.Substring(i + 1);
//
//             string res = await handler.ReceiveQuery(new ReceivedQuery(command, content));
//             e.Query.Reply(res);
//         }
//         
//         //コマンド名と引数名の区切り文字のインデックスを探します。
//         private static int FindColonCharIndex(string s)
//         {
//             for (int i = 0; i < s.Length; i++)
//             {
//                 if (s[i] == ':')
//                 {
//                     return i;
//                 }
//             }
//             return -1;
//         }
//
//     }
// }
