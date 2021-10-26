using System;
using System.Windows;
using System.Windows.Threading;

namespace Baku.VMagicMirrorConfig
{
    class AppExitFromUnityMessage
    {
        public void Initialize(IMessageReceiver receiver)
        {
            receiver.ReceivedCommand += OnReceivedCommand;
        }

        private void OnReceivedCommand(object? sender, CommandReceivedEventArgs e)
        {
            switch (e.Command)
            {
                case ReceiveMessageNames.CloseConfigWindow:
                    Application.Current.Dispatcher?.BeginInvoke(
                        new Action(() => Application.Current.MainWindow.Close()),
                        DispatcherPriority.ApplicationIdle
                        );
                    break;
                default:
                    break;
            }
        }
    }
}
