namespace Nodes.Generate;

public sealed class GeneratePayload : Payload
{
    public override string Type => "generate";
    
    public GeneratePayload(string type)
    {
        if (type != Type)
        {
            throw new MessageDeserializationTypeMismatchException();
        }
    }
}