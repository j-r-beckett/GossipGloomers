using System.Reflection;
using Newtonsoft.Json;

namespace Nodes;

public abstract class Node
{
    protected string? _nodeId;
    private dynamic? _context;
    
    protected void Send(dynamic msg)
    {
        var msgJson = JsonConvert.SerializeObject(msg);
        Log($"sending msg {msgJson}");
        Console.WriteLine(msgJson);
    }

    public void HandleMessage(MethodInfo handler, dynamic msg)
    {
        _context = msg;
        handler.Invoke(this, new[] { msg });
        _context = null;
    }

    public void Reply<T>(T payload) where T: Payload
    {
        Send(new Message<T>(_nodeId, _context.Src, payload));
    }

    protected void Log(string s) => Console.Error.WriteLine(s);

    public void ReceiveMessage(Message<InitPayload> msg)
    {
        _nodeId = msg.Body.NodeId;
        Reply(new InitOkPayload(msg.Body.MsgId));
    }
}