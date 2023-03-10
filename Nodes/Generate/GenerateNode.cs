using Newtonsoft.Json;
using Nodes.Echo;

namespace Nodes.Generate;

public class GenerateNode
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

    public void ReceiveMessage(Message<GeneratePayload> msg)
    {
        var payload = new GenerateOkPayload(Guid.NewGuid().ToString(), msg.Body.MsgId);
        Send(new Message<GenerateOkPayload>(_nodeId, msg.Src, payload));
    }
}