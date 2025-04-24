using Baku.VMagicMirror;
using System.Diagnostics;
using System.Linq;

namespace Baku.VMagicMirrorConfig
{
    internal class UnityAppCloser
    {
        private int _unityProcessId = -1;

        public UnityAppCloser(IMessageReceiver receiver)
        {
            receiver.ReceivedCommand += (e) =>
            {
                if (e.Command is VmmServerCommands.SetUnityProcessId &&
                    int.TryParse(e.GetStringValue(), out int processId)
                    )
                {
                    _unityProcessId = processId;
                }
            };
        }

        public void Close()
        {
            if (_unityProcessId < 0)
            {
                return;
            }

            //NOTE: IDベースで判定することで、多重起動しているとき間違ったウィンドウを閉じないようにしてます
            Process.GetProcesses()
                .FirstOrDefault(p => p.Id == _unityProcessId)
                ?.CloseMainWindow();
        }
    }
}
