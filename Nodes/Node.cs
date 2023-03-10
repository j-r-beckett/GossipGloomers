using Newtonsoft.Json;

namespace Nodes;

public class Node
{
    protected string? _nodeId;

    protected void Send(dynamic msg)
    {
        var msgJson = JsonConvert.SerializeObject(msg);
        Log($"sending msg {msgJson}");
        Console.WriteLine(msgJson);
    }

    protected void Log(string s) => Console.Error.WriteLine(s);

    public void ReceiveMessage(Message<InitPayload> msg)
    {
        _nodeId = msg.Body.NodeId;
        Send(new Message<InitOkPayload>(_nodeId, msg.Src, new InitOkPayload(msg.Body.MsgId)));
    }
}