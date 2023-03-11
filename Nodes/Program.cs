using Nodes.Broadcast;
using Nodes.Echo;
using Nodes.Generate;

var node = new SingletonBroadcastNode();

for (string? line = null;; line = Console.In.ReadLine())
{
    if (line != null)
    {
        node.ProcessMessage(line);
    }

    Thread.Sleep(10);
}