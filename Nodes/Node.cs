using System.Reflection;
using Newtonsoft.Json;

namespace Nodes;

public abstract class Node
{
    protected string? _nodeId;
    protected string[] _nodeIds;
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

    public void Reply(dynamic payload)
    {
        Send(new { Src = _nodeId, Dest = _context.Src, Body = payload });
    }

    protected void Log(string s) => Console.Error.WriteLine(s);

    [MessageType("init")]
    public void ReceiveInit(dynamic msg)
    {
        _nodeId = msg.Body.NodeId;
        _nodeIds = msg.Body.NodeIds.ToObject<string[]>();
        Reply(new { Type = "init_ok", InReplyTo = msg.Body.MsgId });
    }

    public virtual void PerformBackgroundTasks()
    {
    }

    public void ProcessMessage(string msgStr)
    {
        Console.Error.WriteLine($"processing msg {msgStr}");
        var msg = MessageParser.ParseMessage(msgStr);
        var msgType = (string)msg.Body.Type;
        var handlers = GetType()
            .GetMethods()
            .Where(m => m.GetCustomAttributes()
                .Any(attr => (attr as MessageTypeAttribute)?.MessageType.ToString() == msgType));
        foreach (var handler in handlers)
        {
            _context = msg;
            handler.Invoke(this, new object[] { msg });
            _context = null;
        }
    }
}