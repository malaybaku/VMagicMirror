using System;
using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class ReceivedMessageHandler : MonoBehaviour
    {
        [SerializeField]
        ViewerWithReceiver viewer = null;

        [SerializeField]
        BackgroundColorController bgController = null;

        private MessageHandler[] _handlers;

        private void Start()
        {
            _handlers = new MessageHandler[]
            {
                new MessageHandler(Messages.OpenVrmPreview, path =>
                {
                    viewer.LoadModelOnlyForPreview(path);
                }),
                new MessageHandler(Messages.OpenVrm, path =>
                {
                    viewer.LoadModel(path);
                }),
                new MessageHandler(Messages.UpdateChromakey, c =>
                {
                    float[] argb = c.Split(',')
                        .Select(e => int.Parse(e) / 255.0f)
                        .ToArray();
                    bgController.ChangeColor(argb[0], argb[1], argb[2], argb[3]);
                }),
            };
        }

        public void Receive(string message)
        {
            string command = message.Split(':')[0];
            _handlers
                .FirstOrDefault(h => h.Command == command)
                ?.Action
                ?.Invoke(message.Substring(command.Length + 1));
        }

        class MessageHandler
        { 
            public MessageHandler(string command, Action<string> action)
            {
                Command = command;
                Action = action;

            }

            public string Command { get; }
            public Action<string> Action { get; }

        }

        static class Messages
        {
            public static string OpenVrmPreview => nameof(OpenVrmPreview);
            public static string OpenVrm => nameof(OpenVrm);
            public static string UpdateChromakey => nameof(UpdateChromakey);
        } 
    }
}

