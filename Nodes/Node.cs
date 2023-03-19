using System.Reflection;

namespace Nodes;

public abstract class Node
{
    private dynamic? _context;
    protected string? _nodeId;
    protected string[] _nodeIds;

    public void ProcessMessage(string msgStr)
    {
        Console.Error.WriteLine($"processing msg {msgStr}");
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
        _nodeId = msg.Body.NodeId;
        _nodeIds = msg.Body.NodeIds.ToObject<string[]>();
        Reply(new { Type = "init_ok", InReplyTo = msg.Body.MsgId });
    }

    protected void Reply(dynamic payload)
    {
        MaelstromUtils.Send(new { Src = _nodeId, Dest = _context.Src, Body = payload });
    }
}