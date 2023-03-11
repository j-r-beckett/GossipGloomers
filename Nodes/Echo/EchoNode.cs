namespace Nodes.Echo;

public class EchoNode : Node
{
    [MessageHandler("echo")]
    public void HandleEcho(dynamic msg)
        => Reply(new { Type = "echo_ok", Echo = msg.Body.Echo, InReplyTo = msg.Body.MsgId });
}