namespace Nodes.Broadcast;

public class SingletonBroadcastNode : Node
{
    private readonly List<long> _messages = new();

    [MessageType("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        _messages.Add((int) msg.Body.Message);
        Reply(new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageType("read")]
    public void HandleRead(dynamic msg)
    {
        Reply(new { Type = "read_ok", Messages = _messages, InReplyTo = msg.Body.MsgId });
    }

    [MessageType("topology")]
    public void HandleTopology(dynamic msg)
    {
        Reply(new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
    }
}