namespace Nodes;

[AttributeUsage(System.AttributeTargets.Method)]  
public class MessageHandlerAttribute : Attribute
{
    public string MessageType { get; }

    public MessageHandlerAttribute(string messageType) => MessageType = messageType;
}  