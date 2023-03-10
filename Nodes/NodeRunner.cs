using System.Reflection;
using Newtonsoft.Json;

namespace Nodes;

public class NodeRunner<T> where T : Node
{
    private readonly T _node;
    private readonly Type[] _registeredTypes;

    public NodeRunner(T node)
    {
        _node = node;
        _registeredTypes = _node.GetType()
            .GetMethods()
            .Where(m => m.Name == nameof(_node.ReceiveMessage) && m.GetParameters().Length == 1)
            .Select(m => m.GetParameters().First().ParameterType)
            .ToArray();
    }

    public void ProcessMessage(string msgStr)
    {
        foreach (var type in _registeredTypes)
        {
            try
            {
                var deserializeMethod = typeof(JsonConvert)
                    .GetMethod(nameof(JsonConvert.DeserializeObject), 1, new[] { typeof(string) })
                    .MakeGenericMethod(type);
                var msg = deserializeMethod.Invoke(null, new object[] { msgStr });
                var receiveMethod = _node.GetType().GetMethod(nameof(_node.ReceiveMessage), new[] { type });
                receiveMethod.Invoke(_node, new [] { msg });
            }
            catch (TargetInvocationException ex) when (ex.InnerException is MessageDeserializationTypeMismatchException)
            {
                // do nothing
            }
        }
    }
}