using Nodes.Echo;

namespace Nodes.Broadcast;

public class SingleBroadcastNode : Node
{
    private List<int> _messages = new();
    private List<string> _adjacentNodes = new();

    public void ReceiveMessage(Message<BroadcastPayload> msg)
    {
        Log($"received broadcast message {msg}");
        _messages.Add(msg.Body.Message);
        Reply(new BroadcastOkPayload(msg.Body.MsgId));
    }

    public void ReceiveMessage(Message<ReadPayload> msg)
    {
        Log($"received read message {msg}");
        Reply(new ReadOkPayload(msg.Body.MsgId, _messages));
    }

    public void ReceiveMessage(Message<TopologyPayload> msg)
    {
        Log($"received topology message {msg}");
        if (!msg.Body.Topology.TryGetValue(_nodeId, out _adjacentNodes))
        {
            throw new ArgumentException("Node is not part of topology");
        }

        Reply(new TopologyOkPayload(msg.Body.MsgId));
    }
}