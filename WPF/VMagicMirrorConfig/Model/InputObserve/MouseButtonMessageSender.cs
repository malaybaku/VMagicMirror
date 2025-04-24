﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Baku.VMagicMirrorConfig
{
    internal class MouseButtonMessageSender : IDisposable
    {
        //NOTE: value側の文字列がUnity側とのコントラクトであることに注意
        private static readonly Dictionary<int, string> MouseEventNumberToEventName = new Dictionary<int, string>()
        {
            [MouseMessages.WM_LBUTTONDOWN] = "LDown",
            [MouseMessages.WM_LBUTTONUP] = "LUp",
            [MouseMessages.WM_RBUTTONDOWN] = "RDown",
            [MouseMessages.WM_RBUTTONUP] = "RUp",
            [MouseMessages.WM_MBUTTONDOWN] = "MDown",
            [MouseMessages.WM_MBUTTONUP] = "MUp",
        };

        public MouseButtonMessageSender(IMessageSender sender)
        {
            _sender = sender;
            _mouseHook.MouseButton += OnMouseButton;
        }

        private void OnMouseButton(int code)
        {
            if (MouseEventNumberToEventName.TryGetValue(code, out var name))
            {
                _sender.SendMessage(MessageFactory.MouseButton(name));
            }
        }

        private readonly MouseHook _mouseHook = new MouseHook();
        private readonly IMessageSender _sender;
        private readonly MessageLoopThread _messageLoopThread = new MessageLoopThread();

        public void Start() => _messageLoopThread.Run(
            () => _mouseHook.Start(),
            () => _mouseHook.Dispose()
        );

        public void Dispose() => _messageLoopThread.Stop();
    }
}
