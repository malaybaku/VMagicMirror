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

        [SerializeField]
        InputDeviceReceiver inputDeviceReceiver = null;

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

                new MessageHandler(Messages.KeyDown, c =>
                {
                    inputDeviceReceiver.UpdateKeycodeIndication(c);
                }),

                new MessageHandler(Messages.MouseButton, c =>
                {
                    inputDeviceReceiver.UpdateMouseButton(c);
                }),

                //new MessageHandler(Messages.MouseMoved, c =>
                //{
                //    int[] xy = c.Split(',')
                //        .Select(v => int.Parse(v))
                //        .ToArray();
                //    inputDeviceReceiver.UpdatePositionIndication(xy[0], xy[1]);
                //}),
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

        static class Messages
        {
            public static string OpenVrmPreview => nameof(OpenVrmPreview);
            public static string OpenVrm => nameof(OpenVrm);
            public static string UpdateChromakey => nameof(UpdateChromakey);

            public static string KeyDown => nameof(KeyDown);
            public static string MouseMoved => nameof(MouseMoved);
            public static string MouseButton => nameof(MouseButton);
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

    }
}

