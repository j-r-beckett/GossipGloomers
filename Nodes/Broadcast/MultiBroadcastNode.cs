using System.Collections.Concurrent;
using System.Diagnostics;

namespace Nodes.Broadcast;

public class MultiBroadcastNode : Node
{
    // Used as a HashSet
    private readonly ConcurrentDictionary<long, byte> _messages = new();

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
        _messages[(long)msg.Body.Message] = 0;

        if (IsClientBroadcast(msg))
        {
            foreach (var adjNode in NodeIds)
            {
                if (adjNode != NodeId)
                {
                    Send(new
                    {
                        Src = NodeId,
                        Dest = adjNode,
                        Body = new { Type = "broadcast", msg.Body.Message, MsgId = Next(ref _messageId) }
                    });
                }
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
            Messages = _messages.Keys.OrderBy(n => n).ToList(), // sort to make it easier to read output
            InReplyTo = msg.Body.MsgId
        });
    }

    [MessageHandler("topology")]
    public void HandleTopology(dynamic msg)
    {
        Respond(msg, new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
    }
}