namespace Nodes.Broadcast;

public class FaultTolerantBroadcastNode : Node
{
    private readonly HashSet<long> _messages = new();
    private readonly Dictionary<long, dynamic> _pendingUpdates = new();

    private long _messageId = -1;

    [MessageHandler("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        _messages.Add((long)msg.Body.Message);
        
        if (((string)msg.Src).StartsWith("c"))
        {
            foreach (var nodeId in NodeIds)
            {
                if (nodeId != NodeId)
                {
                    Send(new
                        {
                            Src = NodeId,
                            Dest = nodeId,
                            Body = new { Type = "broadcast", msg.Body.Message, MsgId = ++_messageId }
                        })
                        .EnableRetry(1500);
                }
            }
        }

        Reply(msg, new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("read")]
    public void HandleRead(dynamic msg)
    {
        Reply(msg, new
        {
            Type = "read_ok",
            Messages = _messages.AsEnumerable().OrderBy(n => n).ToList(), // sort to make it easier to read output
            InReplyTo = msg.Body.MsgId
        });
    }

    [MessageHandler("topology")]
    public void HandleTopology(dynamic msg)
    {
        Reply(msg, new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
    }
}