using System;
using Newtonsoft.Json;

namespace Nodes.Echo;

public class EchoNode
{
    private string? _nodeId;

    private void Send(dynamic msg)
    {
        Console.WriteLine(JsonConvert.SerializeObject(msg));
    }

    private void Log(string s)
    {
        Console.Error.WriteLine(s);
    }

    public void ReceiveMessage(Message<InitPayload> msg)
    {
        _nodeId = msg.Body.NodeId;
        var payload = new InitOkPayload(msg.Body.MsgId);
        Send(new Message<InitOkPayload>(_nodeId, msg.Src, payload));
    }

    public void ReceiveMessage(Message<EchoPayload> msg)
    {
        var payload = new EchoOkPayload(msg.Body.MsgId, msg.Body.MsgId, msg.Body.Echo);
        Send(new Message<EchoOkPayload>(_nodeId, msg.Src, payload));
    }
}