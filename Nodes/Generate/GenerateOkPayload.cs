namespace Nodes.Generate;

public class GenerateOkPayload : Payload
{
    public override string Type => "generate_ok";
    public string Id { get; }
    public int InReplyTo { get; }

    public GenerateOkPayload(string id, int inReplyTo) => (Id, InReplyTo) = (id, inReplyTo);
}