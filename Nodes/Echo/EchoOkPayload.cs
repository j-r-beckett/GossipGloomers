namespace Nodes.Echo;

public sealed class EchoOkPayload : Payload
{
    public override string Type => "echo_ok";
    public int InReplyTo { get; }
    public string Echo { get; }

    public EchoOkPayload(int inReplyTo, string echo) => (InReplyTo, Echo) = (inReplyTo, echo);
}