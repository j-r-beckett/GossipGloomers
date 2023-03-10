namespace Nodes.Echo;

public class ReadPayload : Payload
{
    public override string Type => "read";
    public int MsgId { get; }

    public ReadPayload(string type, int msgId)
    {
        if (type != Type)
        {
            throw new MessageDeserializationTypeMismatchException();
        }

        MsgId = msgId;
    }
}