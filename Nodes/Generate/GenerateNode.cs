namespace Nodes.Generate;

public class GenerateNode : Node
{
    public void ReceiveMessage(Message<GeneratePayload> msg)
        => Reply(new GenerateOkPayload(Guid.NewGuid().ToString(), msg.Body.MsgId));
}