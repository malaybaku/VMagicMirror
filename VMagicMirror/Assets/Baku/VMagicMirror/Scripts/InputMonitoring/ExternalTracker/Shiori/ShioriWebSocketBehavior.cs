using Baku.VMagicMirror.ExternalTracker.Shiori;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class ShioriWebSocketBehavior : WebSocketBehavior
{
    private static ShioriReceiver ShioriReceiver; // you need to store reference of this form to use it

    public ShioriWebSocketBehavior(ShioriReceiver _ShioriReceiver)
    {
        ShioriReceiver = _ShioriReceiver;
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        var message = Encoding.ASCII.GetString(e.RawData);
        ShioriReceiver.RawMessage = message;
    }
}
