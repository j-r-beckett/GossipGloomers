namespace Nodes;

public class InitNode : Node
{
    [MessageHandler("init")]
    public void HandleInit(dynamic msg)
    {
        NodeId = msg.Body.NodeId;
        NodeIds = msg.Body.NodeIds.ToObject<string[]>();
        WriteResponse(msg, new { Type = "init_ok", InReplyTo = msg.Body.MsgId });
    }
}