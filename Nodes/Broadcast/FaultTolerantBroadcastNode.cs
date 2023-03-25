namespace Nodes.Broadcast;

public class FaultTolerantBroadcastNode : Node
{
    private readonly HashSet<long> _messages = new();
    private readonly Dictionary<long, dynamic> _pendingUpdates = new();

    private long _messageId;

    private static long Next(ref long messageId)
    {
        return ++messageId;
    }

    private static bool IsClientBroadcast(dynamic broadcastMsg)
    {
        return broadcastMsg.Src.ToString().StartsWith("c");
    }

    [BackgroundProcess(50)]
    public void ResendPendingUpdates()
    {
        foreach (var update in _pendingUpdates.Values) MaelstromUtils.Send(update);
    }


    [MessageHandler("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        if (IsClientBroadcast(msg))
            foreach (var adjNode in NodeIds)
                if (adjNode != NodeId)
                {
                    var update = new
                    {
                        Src = NodeId,
                        Dest = adjNode,
                        Body = new { Type = "broadcast", msg.Body.Message, MsgId = Next(ref _messageId) }
                    };
                    _pendingUpdates.Add(_messageId, update);
                    MaelstromUtils.Send(update);
                }

        _messages.Add((long)msg.Body.Message);
        Reply(new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("broadcast_ok")]
    public void HandleBroadcastOk(dynamic msg)
    {
        _pendingUpdates.Remove((long)msg.Body.InReplyTo);
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