using Nodes.Broadcast;

var node = new MultiBroadcastNode();

for (string? line = null;; line = Console.In.ReadLine())
{
    if (line != null)
    {
        node.ProcessMessage(line);
    }

    Thread.Sleep(10);
}