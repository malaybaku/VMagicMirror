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
        BackgroundController bgController = null;

        [SerializeField]
        InputDeviceReceiver inputDeviceReceiver = null;

        [SerializeField]
        WaitMotionReceiver waitMotionReceiver = null;

        [SerializeField]
        Canvas metaDataCanvas = null;

        [SerializeField]
        LightingController lightingController = null;
        //Light mainLight = null;

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
                new MessageHandler(Messages.CancelLoadVrm, c =>
                {
                    metaDataCanvas.enabled = false;
                }),

                #endregion

                #region 背景色と光とウィンドウ周り

                new MessageHandler(Messages.Chromakey, c =>
                {
                    int[] argb = c.Split(',')
                        .Select(e => int.Parse(e))
                        .ToArray();
                    bgController.ChangeColor(argb[0], argb[1], argb[2], argb[3]);
                }),

                new MessageHandler(Messages.LightIntensity, c =>
                {
                    lightingController.SetLightIntensity(Percentage(c));
                }),

                new MessageHandler(Messages.LightColor, c =>
                {
                    float[] rgb = c.Split(',')
                        .Select(v => int.Parse(v) / 255.0f)
                        .ToArray();
                    lightingController.SetLightColor(rgb[0], rgb[1], rgb[2]);
                }),

                new MessageHandler(Messages.BloomIntensity, c =>
                {
                    lightingController.SetBloomIntensity(Percentage(c));
                }),

                new MessageHandler(Messages.BloomThreshold, c =>
                {
                    lightingController.SetBloomThreshold(Percentage(c));
                }),

                new MessageHandler(Messages.BloomColor, c =>
                {
                    float[] rgb = c.Split(',')
                        .Select(v => int.Parse(v) / 255.0f)
                        .ToArray();
                    lightingController.SetBloomColor(rgb[0], rgb[1], rgb[2]);
                }),

                #endregion

                #region ウィンドウ

                new MessageHandler(Messages.WindowFrameVisibility, c =>
                {
                    bgController.SetWindowFrameVisibility(bool.Parse(c));
                }),

                new MessageHandler(Messages.IgnoreMouse, c =>
                {
                    bgController.SetIgnoreMouseInput(bool.Parse(c));
                }),

                new MessageHandler(Messages.TopMost, c =>
                {
                    bgController.SetTopMost(bool.Parse(c));
                }),

                new MessageHandler(Messages.WindowDraggable, c =>
                {
                    bgController.SetWindowDraggable(bool.Parse(c));
                }),

                new MessageHandler(Messages.MoveWindow, c =>
                {
                    int[] xy = c.Split(',').Select(v => int.Parse(v)).ToArray();
                    bgController.MoveWindow(xy[0], xy[1]);
                }),

                #endregion

                #region レイアウト: キャラ体型

                new MessageHandler(Messages.LengthFromWristToPalm, c =>
                {
                    inputDeviceReceiver.SetLengthFromWristToPalm(Centimeter(c));
                }),

                new MessageHandler(Messages.LengthFromWristToTip, c =>
                {
                    inputDeviceReceiver.SetLengthFromWristToTip(Centimeter(c));
                }),

                new MessageHandler(Messages.HandYOffsetBasic, c =>
                {
                    inputDeviceReceiver.SetHandYOffsetBasic(Centimeter(c));
                }),

                new MessageHandler(Messages.HandYOffsetAfterKeyDown, c =>
                {
                    inputDeviceReceiver.SetHandYOffsetAfterKeyDown(Centimeter(c));
                }),

                #endregion

                #region レイアウト: キャラの動きについて

                new MessageHandler(Messages.EnableWaitMotion, c =>
                {
                    waitMotionReceiver.EnableWaitMotion(bool.Parse(c));
                }),

                new MessageHandler(Messages.WaitMotionScale, c =>
                {
                    waitMotionReceiver.SetWaitMotionScale(Percentage(c));
                }),

                new MessageHandler(Messages.WaitMotionPeriod, c =>
                {
                    //秒単位で送られてくる点に注意
                    waitMotionReceiver.SetWaitMotionDuration(int.Parse(c));
                }),

                new MessageHandler(Messages.EnableTouchTyping, c =>
                {
                    inputDeviceReceiver.EnableTouchTypingHeadMotion(bool.Parse(c));
                }),

                #endregion

                #region レイアウト: カメラ配置

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

                #endregion

                #region レイアウト: HID配置

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
            public static string CancelLoadVrm => nameof(CancelLoadVrm);

            public static string Chromakey => nameof(Chromakey);
            public static string LightIntensity = nameof(LightIntensity);
            public static string LightColor => nameof(LightColor);
            public static string BloomIntensity => nameof(BloomIntensity);
            public static string BloomThreshold => nameof(BloomThreshold);
            public static string BloomColor => nameof(BloomColor);

            public static string WindowFrameVisibility => nameof(WindowFrameVisibility);
            public static string IgnoreMouse => nameof(IgnoreMouse);
            public static string TopMost => nameof(TopMost);
            public static string WindowDraggable => nameof(WindowDraggable);
            public static string MoveWindow => nameof(MoveWindow);

            public static string LengthFromWristToTip => nameof(LengthFromWristToTip);
            public static string LengthFromWristToPalm => nameof(LengthFromWristToPalm);
            public static string HandYOffsetBasic => nameof(HandYOffsetBasic);
            public static string HandYOffsetAfterKeyDown => nameof(HandYOffsetAfterKeyDown);

            public static string EnableTouchTyping => nameof(EnableTouchTyping);

            public static string CameraHeight => nameof(CameraHeight);
            public static string CameraDistance => nameof(CameraDistance);
            public static string CameraVerticalAngle => nameof(CameraVerticalAngle);

            public static string HidHeight => nameof(HidHeight);
            public static string HidHorizontalScale => nameof(HidHorizontalScale);
            public static string HidVisibility => nameof(HidVisibility);

            public static string EnableWaitMotion => nameof(EnableWaitMotion);
            public static string WaitMotionScale => nameof(WaitMotionScale);
            public static string WaitMotionPeriod => nameof(WaitMotionPeriod);
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

