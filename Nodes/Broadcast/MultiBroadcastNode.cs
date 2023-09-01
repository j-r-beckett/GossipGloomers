using System.Diagnostics;

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
    public async void HandleBroadcast(dynamic msg)
    {
        _messages.Add((int)msg.Body.Message);

        if (IsClientBroadcast(msg))
        {
            foreach (var adjNode in NodeIds)
            {
                if (adjNode != NodeId)
                {
                    SendAndWait(new
                    {
                        Src = NodeId,
                        Dest = adjNode,
                        Body = new { Type = "broadcast", msg.Body.Message, MsgId = Next(ref _messageId) }
                    });
                }
            }
        }
        
        Reply(msg, new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("read")]
    public async void HandleRead(dynamic msg)
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