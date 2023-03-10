using Nodes.Echo;

namespace Nodes.Broadcast;

public class BroadcastNode : Node
{
    private List<int> _messages = new();
    private List<string> _adjacentNodes = new();

    public void ReceiveMessage(Message<BroadcastPayload> msg)
    {
        Log($"received broadcast message {msg}");
        _messages.Add(msg.Body.Message);
        Send(new Message<BroadcastOkPayload>(_nodeId, msg.Src, new BroadcastOkPayload(msg.Body.MsgId)));
    }

    public void ReceiveMessage(Message<ReadPayload> msg)
    {
        Log($"received read message {msg}");
        Send(new Message<ReadOkPayload>(_nodeId, msg.Src,
            new ReadOkPayload(msg.Body.MsgId, _messages)));
    }

    public void ReceiveMessage(Message<TopologyPayload> msg)
    {
        Log($"received topology message {msg}");
        if (!msg.Body.Topology.TryGetValue(_nodeId, out _adjacentNodes))
        {
            throw new ArgumentException("Node is not part of topology");
        }

        Send(new Message<TopologyOkPayload>(_nodeId, msg.Src, new TopologyOkPayload(msg.Body.MsgId)));
    }
}