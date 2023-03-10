namespace Nodes.Broadcast;

public class BroadcastPayload : Payload
{
    public override string Type => "broadcast";
    public int MsgId { get; }
    public int Message { get; }

    public BroadcastPayload(string type, int msgId, int message)
    {
        if (type != Type)
        {
            throw new MessageDeserializationTypeMismatchException();
        }

        MsgId = msgId;
        Message = message;
    }
}