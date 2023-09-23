using static Nodes.IO;

namespace Nodes.Generate;

public class GenerateIdNode : InitNode
{
    [MessageHandler("generate")]
    public void HandleGenerate(dynamic msg)
    {
        WriteResponse(msg,new { Type = "generate_ok", Id = Guid.NewGuid().ToString(), InReplyTo = msg.Body.MsgId });
    }
}