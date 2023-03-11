namespace Nodes.Broadcast;

public class MultiBroadcastNode : Node
{
    private HashSet<string> _adjacentNodes = new ();
    private readonly HashSet<long> _messages = new();

    private int _currentMessageId;

    private int NextMessageId() => ++_currentMessageId;

    [MessageType("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        if (!_messages.Contains((int)msg.Body.Message))
        {
            foreach (var adjNode in _adjacentNodes)
            {
                Send(new
                {
                    Src = _nodeId,
                    Dest = adjNode,
                    Body = new { Type = "broadcast", message = msg.Body.Message, MsgId = NextMessageId() }
                });
            }
        }

        _messages.Add((int)msg.Body.Message);
        Reply(new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageType("broadcast_ok")]
    public void HandleBroadcastOk(dynamic msg)
    {
    }

    [MessageType("read")]
    public void HandleRead(dynamic msg)
        => Reply(new { Type = "read_ok", Messages = _messages, InReplyTo = msg.Body.MsgId });

    [MessageType("topology")]
    public void HandleTopology(dynamic msg)
    {
        Dictionary<string, HashSet<string>> topology = msg.Body.Topology.ToObject<Dictionary<string, HashSet<string>>>();
        _adjacentNodes = topology[_nodeId.ToUpper()];
        Reply(new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
    }
}