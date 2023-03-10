using Newtonsoft.Json;

namespace Nodes;

public class Message<T> where T:Payload
{
    public string Src { init; get; }
    public string Dest { init; get; }
    public T Body { init; get; }

    public Message(string src, string dest, T body) => (Src, Dest, Body) = (src, dest, body);

    public override string ToString() => JsonConvert.SerializeObject(this);
}