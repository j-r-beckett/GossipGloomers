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

var nodeRunner = new NodeRunner<BroadcastNode>(new BroadcastNode());
while (true)
{
    var line = Console.In.ReadLine();
    if (line != null)
    {
        nodeRunner.ProcessMessage(line);
    }
    Thread.Sleep(10);
}