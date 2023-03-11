using System.Runtime.Serialization;

namespace Nodes.Broadcast;

public class MultiBroadcastNode : Node
{
    private readonly HashSet<long> _messages = new();

    private int _messageId;

    private static int Next(ref int messageId) => ++messageId;

    private static bool IsClientBroadcast(dynamic broadcastMsg) => broadcastMsg.Src.ToString().StartsWith("c");


    [MessageType("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        _messages.Add((int)msg.Body.Message);

        if (IsClientBroadcast(msg))
        {
            foreach (var adjNode in _nodeIds)
            {
                if (adjNode != _nodeId)
                {
                    Send(new
                    {
                        Src = _nodeId,
                        Dest = adjNode,
                        Body = new { Type = "broadcast", Message = msg.Body.Message, MsgId = Next(ref _messageId) }
                    });
                }
            }

            Reply(new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
        }
    }

    [MessageType("broadcast_ok")]
    public void HandleBroadcastOk(dynamic msg)
    {
    }

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