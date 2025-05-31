using Baku.VMagicMirror;
using Newtonsoft.Json.Linq;
using System;
using System.Windows;

namespace Baku.VMagicMirrorConfig
{
    /// <summary> Unityからエラー情報が飛んできたら表示するクラス </summary>
    class ErrorMessageIndicator
    {
        public ErrorMessageIndicator(IMessageReceiver receiver)
        {
            receiver.ReceivedCommand += OnReceiveCommand;
        }

        private void OnReceiveCommand(CommandReceivedData e)
        {
            if (e.Command is not VmmServerCommands.RequestShowError)
            {
                return;
            }

            try
            {
                var jobj = JObject.Parse(e.GetStringValue());
                var rawLevel = jobj["level"];
                var rawContent = jobj["content"];
                var rawTitle = jobj["title"];

                if (rawLevel == null || rawContent == null || rawTitle == null)
                {
                    return;
                }

                var image = (int)rawLevel switch
                {
                    0 => MessageBoxImage.Information,
                    1 => MessageBoxImage.Warning,
                    _ => MessageBoxImage.Error,
                };

                var content = (string?)rawContent ?? "";
                var title = (string?)rawTitle ?? "";
                MessageBox.Show(content, title, MessageBoxButton.OK, image);
            }
            catch (Exception ex)
            {
                LogOutput.Instance.Write(ex);
            }
        }
    }

    /// <summary> 警告の重大度。Infoは普通使わないし、Fatalも今のところ想定外 </summary>
    enum ErrorIndicationLevel
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Fatal = 3,
    }
}
