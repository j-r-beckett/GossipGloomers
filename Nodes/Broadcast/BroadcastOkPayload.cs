namespace Nodes.Broadcast;

public class BroadcastOkPayload : Payload
{
    public override string Type => "broadcast_ok";
    public int InReplyTo { get; }

    public BroadcastOkPayload(int inReplyTo) => InReplyTo = inReplyTo;
}