using Newtonsoft.Json;
using Nodes;

var node = new EchoNode();
while (true)
{
    var line = Console.In.ReadLine();
    if (line != null) node.ReceiveMessage(JsonConvert.DeserializeObject<dynamic>(line));
    Thread.Sleep(10);
}