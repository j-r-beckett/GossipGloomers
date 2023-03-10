namespace Nodes.Echo;

public sealed class EchoOkPayload : Payload
{
    public override string Type => "echo_ok";
    public int MsgId { get; }
    public int InReplyTo { get; }
    public string Echo { get; }

    public EchoOkPayload(int msgId, int inReplyTo, string echo) => (MsgId, InReplyTo, Echo) = (msgId, inReplyTo, echo);
}