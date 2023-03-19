namespace Nodes;

[AttributeUsage(AttributeTargets.Method)]
public class MessageHandlerAttribute : Attribute
{
    public MessageHandlerAttribute(string messageType)
    {
        MessageType = messageType;
    }

    public string MessageType { get; }
}