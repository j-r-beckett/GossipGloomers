namespace Nodes.Transations;

public class Operation
{
    public string Type { get; }
    public int First { get; }
    public int? Second { get; set; }

    public Operation(string[] rawOperation)
    {
        Type = rawOperation[0];
        First = int.Parse(rawOperation[1]);
        Second = rawOperation[2] == null ? null : int.Parse(rawOperation[2]);
    }
        
    public Array ToArray() => new dynamic[] { Type, First, Second.HasValue ? Second : "null" };
        
    public override string ToString() 
        => $"""["{Type}", {First}, {(Second.HasValue ? Second.ToString() : "null")}]""";
}