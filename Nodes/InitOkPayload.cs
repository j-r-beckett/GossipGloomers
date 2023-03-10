namespace Nodes;

public sealed class InitOkPayload : Payload
{
    public override string Type => "init_ok";
    public int InReplyTo { get; }

    public InitOkPayload(int inReplyTo) => InReplyTo = inReplyTo;
}