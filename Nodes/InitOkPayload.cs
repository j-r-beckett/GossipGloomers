namespace Nodes;

public sealed class InitOkPayload : Payload
{
    private const string _type = "init_ok";
    public override string Type => _type;
    public int InReplyTo { get; }

    public InitOkPayload(int inReplyTo) : this(_type, inReplyTo) { }
    public InitOkPayload(string type, int inReplyTo)
    {
        if (type != Type)
        {
            throw new MessageDeserializationTypeMismatchException();
        }
        InReplyTo = inReplyTo;
    }
}