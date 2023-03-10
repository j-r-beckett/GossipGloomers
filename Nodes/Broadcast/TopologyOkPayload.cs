namespace Nodes.Broadcast;

public class TopologyOkPayload : Payload
{
    public override string Type => "topology_ok";
    public int MsgId { get; }
    public int InReplyTo { get; }

    public TopologyOkPayload(int msgId, int inReplyTo) => (MsgId, InReplyTo) = (msgId, inReplyTo);
}