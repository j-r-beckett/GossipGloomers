namespace Nodes;

public sealed class EchoOkPayload : Payload
{
    private const string _type = "echo_ok";
    public override string Type => _type;
    public int MsgId { get; }
    public int InReplyTo { get; }
    public string Echo { get; }

    public EchoOkPayload(int msgId, int inReplyTo, string echo) : this(_type, msgId, inReplyTo, echo) { }
    
    public EchoOkPayload(string type, int msgId, int inReplyTo, string echo)
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