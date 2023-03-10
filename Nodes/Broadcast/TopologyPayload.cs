namespace Nodes.Echo;

public class TopologyPayload : Payload
{
    public override string Type => "topology";
    public int MsgId { get; }
    public Dictionary<string, List<string>> Topology { get; }

    public TopologyPayload(string type, int msgId, Dictionary<string, List<string>> topology)
    {
        if (type != Type)
        {
            throw new MessageDeserializationTypeMismatchException();
        }

        MsgId = msgId;
        Topology = topology;
    }
}