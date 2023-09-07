﻿namespace Nodes.Echo;

public class EchoNode : Node
{
    [MessageHandler("echo")]
    public async void HandleEcho(dynamic msg)
    {
        Respond(msg,new { Type = "echo_ok", msg.Body.Echo, InReplyTo = msg.Body.MsgId });
    }
}