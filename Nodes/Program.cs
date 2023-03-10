using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Nodes;
using Nodes.Generate;

JsonConvert.DefaultSettings = () => new JsonSerializerSettings
{
    ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
};

var nodeRunner = new NodeRunner<GenerateNode>(new GenerateNode());
while (true)
{
    var line = Console.In.ReadLine();
    if (line != null)
    {
        nodeRunner.ProcessMessage(line);
    }
    Thread.Sleep(10);
}