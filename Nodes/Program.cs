using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Nodes;
using Nodes.Broadcast;
using Nodes.Echo;
using Nodes.Generate;

JsonConvert.DefaultSettings = () => new JsonSerializerSettings
{
    ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
};

var node = new SingletonBroadcastNode();
while (true)
{
    var line = Console.In.ReadLine();
    if (line != null)
    {
        node.ProcessMessage(line);
    }
    Thread.Sleep(10);
}