using System.Collections.Immutable;

namespace Nodes.Broadcast;

public class SimpleBroadcastNode : Node
{
    private ImmutableHashSet<long> _messages = ImmutableHashSet<long>.Empty;
    
    private static int Next(ref int messageId) => ++messageId;
    private int _messageId = -1;
    private static bool IsFromClient(dynamic msg) => msg.Src.ToString().ToLower().StartsWith("c");

    [MessageHandler("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        var message = (long)msg.Body.Message;
        ImmutableInterlocked.Update(ref _messages, messages => messages.Add(message));
        if (IsFromClient(msg))
        {
            foreach (var nodeId in NodeIds)
            {
                if (nodeId != NodeId)
                {
                    SendRequest(new
                    {
                        Src = NodeId,
                        Dest = nodeId,
                        Body = new { Type = "broadcast", Message = message, MsgId = Next(ref _messageId) }
                    });
                }
            }
        }

        WriteResponse(msg, new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("read")]
    public void HandleRead(dynamic msg)
    {
        WriteResponse(msg, new
        {
            Type = "read_ok",
            Messages = _messages.AsEnumerable().OrderBy(n => n).ToList(), // sort to make it easier to read output
            InReplyTo = msg.Body.MsgId
        });
    }

    [MessageHandler("topology")]
    public void HandleTopology(dynamic msg)
    {
        WriteResponse(msg, new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
    }
}