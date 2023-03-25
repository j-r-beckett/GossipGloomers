using System.Reflection;
using Newtonsoft.Json;

namespace Nodes;

public abstract class Node
{
    private dynamic? _context;
    public string? NodeId;
    public string[] NodeIds;

    public void ProcessMessage(string msgStr)
    {
        // Console.Error.WriteLine($"processing msg {msgStr}");
        var msg = MessageParser.ParseMessage(msgStr);
        var msgType = (string)msg.Body.Type;
        var handlers = GetType()
            .GetMethods()
            .Where(m => m.GetCustomAttributes()
                .Any(attr => (attr as MessageHandlerAttribute)?.MessageType.ToString() == msgType));
        foreach (var handler in handlers)
        {
            _context = msg;
            handler.Invoke(this, new object[] { msg });
            _context = null;
        }
    }

    [MessageHandler("init")]
    public void HandleInit(dynamic msg)
    {
        NodeId = msg.Body.NodeId;
        NodeIds = msg.Body.NodeIds.ToObject<string[]>();
        Reply(new { Type = "init_ok", InReplyTo = msg.Body.MsgId });
    }

    protected void Reply(dynamic payload)
    {
        Send(new { Src = NodeId, Dest = _context.Src, Body = payload });
    }
    
    public static void Send(dynamic msg)
    {
        var msgJson = JsonConvert.SerializeObject(msg);
        // Log($"sending msg {msgJson}");
        Console.WriteLine(msgJson);
    }
}