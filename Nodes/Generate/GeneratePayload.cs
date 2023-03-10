namespace Nodes.Generate;

public sealed class GeneratePayload : Payload
{
    public override string Type => "generate";
    public int MsgId { get; }
    
    public GeneratePayload(string type, int msgId)
    {
        if (type != Type)
        {
            throw new MessageDeserializationTypeMismatchException();
        }
        MsgId = msgId;
    }
}