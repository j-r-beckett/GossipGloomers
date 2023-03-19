namespace Nodes.Broadcast;

public class MultiBroadcastNode : Node
{
    private readonly HashSet<long> _messages = new();

    private int _messageId;

    private static int Next(ref int messageId)
    {
        return ++messageId;
    }

    private static bool IsClientBroadcast(dynamic broadcastMsg)
    {
        return broadcastMsg.Src.ToString().StartsWith("c");
    }


    [MessageHandler("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        _messages.Add((int)msg.Body.Message);

        if (IsClientBroadcast(msg))
        {
            foreach (var adjNode in _nodeIds)
                if (adjNode != _nodeId)
                    MaelstromUtils.Send(new
                    {
                        Src = _nodeId,
                        Dest = adjNode,
                        Body = new { Type = "broadcast", msg.Body.Message, MsgId = Next(ref _messageId) }
                    });

            Reply(new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
        }
    }

    [MessageHandler("broadcast_ok")]
    public void HandleBroadcastOk(dynamic msg)
    {
    }

    [MessageHandler("read")]
    public void HandleRead(dynamic msg)
    {
        Reply(new
        {
            Type = "read_ok",
            Messages = _messages.AsEnumerable().OrderBy(n => n).ToList(), // sort to make it easier to read output
            InReplyTo = msg.Body.MsgId
        });
    }

    [MessageHandler("topology")]
    public void HandleTopology(dynamic msg)
    {
        Reply(new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
    }
}