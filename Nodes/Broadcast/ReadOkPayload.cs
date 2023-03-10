namespace Nodes.Broadcast;

public class ReadOkPayload : Payload
{
    public override string Type => "read_ok";
    public int InReplyTo { get; }
    public List<int> Messages { get; }

    public ReadOkPayload(int inReplyTo, List<int> messages)
        => (InReplyTo, Messages) = (inReplyTo, messages);
}