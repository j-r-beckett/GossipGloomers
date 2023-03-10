namespace Nodes.Broadcast;

public class BroadcastOkPayload : Payload
{
    public override string Type => "broadcast_ok";
    public int MsgId { get; }
    public int InReplyTo { get; }

    public BroadcastOkPayload(int msgId, int inReplyTo) => (MsgId, InReplyTo) = (msgId, inReplyTo);
}