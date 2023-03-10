namespace Nodes;

public sealed class InitPayload : Payload
{
    public override string Type => "init";
    public int MsgId { get; }
    public string NodeId { get; }
    public string[] NodeIds { get; }

    public InitPayload(string type, int msgId, string nodeId, string[] nodeIds)
    {
        if (type != Type)
        {
            throw new MessageDeserializationTypeMismatchException();
        }
        MsgId = msgId;
        NodeId = nodeId;
        NodeIds = nodeIds;
    }
}