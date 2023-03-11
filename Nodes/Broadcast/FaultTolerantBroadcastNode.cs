using System.Runtime.Serialization;

namespace Nodes.Broadcast;

public class FaultTolerantBroadcastNode : Node
{
    private readonly HashSet<long> _messages = new();
    private readonly Dictionary<long, dynamic> _pendingUpdates = new();

    private long _messageId;

    private static long Next(ref long messageId) => ++messageId;

    private static bool IsClientBroadcast(dynamic broadcastMsg) => broadcastMsg.Src.ToString().StartsWith("c");
    
    public override void PerformBackgroundTasks()
    {
        foreach (var update in _pendingUpdates.Values)
        {
            Send(update);
        }
    }


    [MessageType("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        if (IsClientBroadcast(msg))
        {
            foreach (var adjNode in _nodeIds)
            {
                if (adjNode != _nodeId)
                {
                    var update = new
                    {
                        Src = _nodeId,
                        Dest = adjNode,
                        Body = new { Type = "broadcast", Message = msg.Body.Message, MsgId = Next(ref _messageId) }
                    };
                    _pendingUpdates.Add(_messageId, update);
                    Send(update);
                }
            }
        }
        
        _messages.Add((long)msg.Body.Message);
        Reply(new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageType("broadcast_ok")]
    public void HandleBroadcastOk(dynamic msg) => _pendingUpdates.Remove((long)msg.Body.InReplyTo);

    [MessageType("read")]
    public void HandleRead(dynamic msg)
        => Reply(new
        {
            Type = "read_ok",
            Messages = _messages.AsEnumerable().OrderBy(n => n).ToList(), // sort to make it easier to read output
            InReplyTo = msg.Body.MsgId
        });

    [MessageType("topology")]
    public void HandleTopology(dynamic msg) => Reply(new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
}