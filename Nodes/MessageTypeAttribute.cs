namespace Nodes;

[AttributeUsage(System.AttributeTargets.Method)]  
public class MessageTypeAttribute : Attribute
{
    public string MessageType { get; }

    public MessageTypeAttribute(string messageType) => MessageType = messageType;
}  