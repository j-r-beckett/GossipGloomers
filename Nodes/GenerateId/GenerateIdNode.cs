namespace Nodes.Generate;

public class GenerateIdNode : Node
{
    [MessageHandler("generate")]
    public void HandleGenerate(dynamic msg)
    {
        WriteResponse(msg,new { Type = "generate_ok", Id = Guid.NewGuid().ToString(), InReplyTo = msg.Body.MsgId });
    }
}