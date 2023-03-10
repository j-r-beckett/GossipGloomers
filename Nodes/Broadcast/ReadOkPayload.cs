namespace Nodes.Broadcast;

public class ReadOkPayload : Payload
{
    public override string Type => "read_ok";
    public int MsgId { get; }
    public int InReplyTo { get; }
    public List<int> Messages { get; }

    public ReadOkPayload(int msgId, int inReplyTo, List<int> messages)
        => (MsgId, InReplyTo, Messages) = (msgId, inReplyTo, messages);
}