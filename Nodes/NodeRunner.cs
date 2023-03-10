using System;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Nodes;
using Nodes.Echo;
using Nodes.Generate;

JsonConvert.DefaultSettings = () => new JsonSerializerSettings
{
    ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
};

var node = new GenerateNode();
var registeredTypes = new[] { typeof(Message<InitPayload>), typeof(Message<GeneratePayload>) };
while (true)
{
    var line = Console.In.ReadLine();
    if (line != null)
    {
        foreach (var type in registeredTypes)
        {
            try
            {
                var deserializeMethod = typeof(JsonConvert)
                    .GetMethod(nameof(JsonConvert.DeserializeObject), 1, new[] { typeof(string) })
                    .MakeGenericMethod(type);
                var msg = deserializeMethod.Invoke(null, new object[] { line });
                var receiveMethod = typeof(GenerateNode).GetMethod(nameof(GenerateNode.ReceiveMessage), new[] { type });
                receiveMethod.Invoke(node, new object[] { msg });
            }
            catch (TargetInvocationException ex) when (ex.InnerException is MessageDeserializationTypeMismatchException)
            {
                // do nothing
            }
        }
    }
    Thread.Sleep(10);
}