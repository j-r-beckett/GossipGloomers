using Newtonsoft.Json;

namespace Nodes;

public class MessageDeserializationTypeMismatchException : JsonSerializationException
{
    public MessageDeserializationTypeMismatchException() { }
    public MessageDeserializationTypeMismatchException(string message) : base(message) { }
}