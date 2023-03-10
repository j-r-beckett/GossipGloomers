namespace Nodes;

public sealed class EchoPayload : Payload
{
    public override string Type => "echo";
    public int MsgId { get; }
    public int InReplyTo { get; }
    public string Echo { get; }
    
    public EchoPayload(string type, int msgId, int inReplyTo, string echo)
    {
        if (type != Type)
        {
            throw new MessageDeserializationTypeMismatchException();
        }
        MsgId = msgId;
        InReplyTo = inReplyTo;
        Echo = echo;
    }
}