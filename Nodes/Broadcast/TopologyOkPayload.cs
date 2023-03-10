namespace Nodes.Broadcast;

public class TopologyOkPayload : Payload
{
    public override string Type => "topology_ok";
    public int InReplyTo { get; }

    public TopologyOkPayload(int inReplyTo) => InReplyTo = inReplyTo;
}