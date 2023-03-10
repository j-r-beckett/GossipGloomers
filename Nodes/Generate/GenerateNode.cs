namespace Nodes.Generate;

public class GenerateNode : Node
{
    public void ReceiveMessage(Message<GeneratePayload> msg) =>
        Send(new Message<GenerateOkPayload>(_nodeId, msg.Src,
            new GenerateOkPayload(Guid.NewGuid().ToString(), msg.Body.MsgId)));
}