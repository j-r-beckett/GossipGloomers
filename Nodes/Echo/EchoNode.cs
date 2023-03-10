namespace Nodes.Echo;

public class EchoNode : Node
{
    public void ReceiveMessage(Message<EchoPayload> msg)
        =>
            // Send(new Message<EchoOkPayload>(_nodeId, msg.Src,
            //     new EchoOkPayload(msg.Body.MsgId, msg.Body.Echo)));
            Reply(new EchoOkPayload(msg.Body.MsgId, msg.Body.Echo));
}