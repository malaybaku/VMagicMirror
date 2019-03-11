using System;
using System.Linq;
using UnityEngine;

namespace Baku.VMagicMirror
{
    public class ReceivedMessageHandler : MonoBehaviour
    {
        [SerializeField]
        VRMLoadController loadController = null;

        [SerializeField]
        BackgroundColorController bgController = null;

        [SerializeField]
        InputDeviceReceiver inputDeviceReceiver = null;

        [SerializeField]
        Canvas metaDataCanvas = null;

        [SerializeField]
        Light mainLight = null;

        [SerializeField]
        Camera cam = null;

        private MessageHandler[] _handlers;

        private void Start()
        {
            _handlers = new MessageHandler[]
            {
                #region キー/マウス入力

                new MessageHandler(Messages.KeyDown, c =>
                {
                    inputDeviceReceiver.ReceiveKeyPressed(c);
                }),

                new MessageHandler(Messages.MouseButton, c =>
                {
                    inputDeviceReceiver.ReceiveMouseButton(c);
                }),

                //new MessageHandler(Messages.MouseMoved, c =>
                //{
                //    int[] xy = c.Split(',')
                //        .Select(v => int.Parse(v))
                //        .ToArray();
                //    inputDeviceReceiver.UpdatePositionIndication(xy[0], xy[1]);
                //}),

                #endregion

                #region VRMのロード

                new MessageHandler(Messages.OpenVrmPreview, path =>
                {
                    metaDataCanvas.enabled = true;
                    loadController.LoadModelOnlyForPreview(path);
                }),
                new MessageHandler(Messages.OpenVrm, path =>
                {
                    metaDataCanvas.enabled = false;
                    loadController.LoadModel(path);
                }),

                #endregion

                #region 背景色と光

                new MessageHandler(Messages.Chromakey, c =>
                {
                    float[] argb = c.Split(',')
                        .Select(e => int.Parse(e) / 255.0f)
                        .ToArray();
                    bgController.ChangeColor(argb[0], argb[1], argb[2], argb[3]);
                }),

                new MessageHandler(Messages.LightIntensity, c =>
                {
                    mainLight.intensity = Percentage(c);
                }),

                #endregion

                #region レイアウト

                new MessageHandler(Messages.LengthFromWristToPalm, c =>
                {
                    inputDeviceReceiver.SetLengthFromWristToPalm(Centimeter(c));
                }),

                new MessageHandler(Messages.LengthFromWristToTip, c =>
                {
                    inputDeviceReceiver.SetLengthFromWristToTip(Centimeter(c));
                }),

                new MessageHandler(Messages.EnableTouchTyping, c =>
                {
                    inputDeviceReceiver.EnableTouchTypingHeadMotion(bool.Parse(c));
                }),

                new MessageHandler(Messages.CameraHeight, c =>
                {
                    cam.transform.position = new Vector3(
                        cam.transform.position.x,
                        Centimeter(c),
                        cam.transform.position.z
                        );
                }),

                new MessageHandler(Messages.CameraDistance, c =>
                {
                    cam.transform.position = new Vector3(
                        cam.transform.position.x,
                        cam.transform.position.y,
                        Centimeter(c)
                        );
                }),

                new MessageHandler(Messages.CameraVerticalAngle, c =>
                {
                    cam.transform.rotation = Quaternion.Euler(int.Parse(c), 180, 0);
                }),

                new MessageHandler(Messages.HidHeight, c =>
                {
                    inputDeviceReceiver.SetHidHeight(
                        Centimeter(c)
                        );
                }),

                new MessageHandler(Messages.HidHorizontalScale, c =>
                {
                    inputDeviceReceiver.SetHidHorizontalScale(
                        Percentage(c)
                        );
                }),

                new MessageHandler(Messages.HidVisibility, c =>
                {
                    inputDeviceReceiver.SetHidVisibility(bool.Parse(c));
                }),

                #endregion

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

        private float Centimeter(string c) => int.Parse(c) * 0.01f;

        private float Percentage(string c) => int.Parse(c) * 0.01f;

        static class Messages
        {
            public static string KeyDown => nameof(KeyDown);
            public static string MouseMoved => nameof(MouseMoved);
            public static string MouseButton => nameof(MouseButton);

            public static string OpenVrmPreview => nameof(OpenVrmPreview);
            public static string OpenVrm => nameof(OpenVrm);

            public static string Chromakey => nameof(Chromakey);
            public static string LightIntensity = nameof(LightIntensity);

            public static string LengthFromWristToTip => nameof(LengthFromWristToTip);
            public static string LengthFromWristToPalm => nameof(LengthFromWristToPalm);
            public static string EnableTouchTyping => nameof(EnableTouchTyping);

            public static string CameraHeight => nameof(CameraHeight);
            public static string CameraDistance => nameof(CameraDistance);
            public static string CameraVerticalAngle => nameof(CameraVerticalAngle);

            public static string HidHeight => nameof(HidHeight);
            public static string HidHorizontalScale => nameof(HidHorizontalScale);
            public static string HidVisibility => nameof(HidVisibility);

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

