using Newtonsoft.Json;

namespace Nodes;

public abstract class Payload
{
    public abstract string Type { get; }
    
    public override string ToString() => JsonConvert.SerializeObject(this);
}