using System;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Nodes;

JsonConvert.DefaultSettings = () => new JsonSerializerSettings
{
    ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
};

var node = new EchoNode();
var registeredTypes = new[] { typeof(Message<InitPayload>), typeof(Message<EchoPayload>) };
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
                var receiveMethod = typeof(EchoNode).GetMethod(nameof(EchoNode.ReceiveMessage), new[] { type });
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