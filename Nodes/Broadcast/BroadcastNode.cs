using System.Collections.Immutable;

namespace Nodes.Broadcast;

public class BroadcastNode : Node
{
    private ImmutableHashSet<long> _messages = ImmutableHashSet<long>.Empty;
    private HashSet<string> _neighbors = new();
    
    private static int Next(ref int messageId) => ++messageId;
    private int _messageId = -1;

    [MessageHandler("update")]
    public void HandleUpdate(dynamic msg)
    {
        HashSet<long> update = msg.Body.Update.ToObject<HashSet<long>>();
        ImmutableInterlocked.Update(ref _messages, messages => messages.Union(update));
    }

    [MessageHandler("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        var message = (int)msg.Body.Message;
        ImmutableInterlocked.Update(ref _messages, messages => messages.Add(message));
        foreach (var nodeId in NodeIds)
        {
            if (nodeId != NodeId)
            {
                WriteMessage(new
                {
                    Src = NodeId,
                    Dest = nodeId,
                    Body = new { Type = "update", Update = _messages, MsgId = Next(ref _messageId) }
                });
            }
        }
        Respond(msg, new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("read")]
    public void HandleRead(dynamic msg)
    {
        Respond(msg, new
        {
            Type = "read_ok",
            Messages = _messages.AsEnumerable().OrderBy(n => n).ToList(), // sort to make it easier to read output
            InReplyTo = msg.Body.MsgId
        });
    }

    [MessageHandler("topology")]
    public void HandleTopology(dynamic msg)
    {
        Respond(msg, new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
    }
}