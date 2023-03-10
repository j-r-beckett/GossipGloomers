namespace Nodes.Generate;

public class GenerateOkPayload : Payload
{
    public override string Type => "generate_ok";
    public int Id { get; }

    public GenerateOkPayload(int id) => Id = id;
}