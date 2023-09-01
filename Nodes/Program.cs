using System.Reflection.Metadata;using Nodes;
using Nodes.Broadcast;
using Nodes.Echo;
using Nodes.GCounter;
using Nodes.Kafka;

void Handle(object sender, UnhandledExceptionEventArgs e)
{
    Console.Error.WriteLine($"args: {e}");
}

AppDomain.CurrentDomain.UnhandledException += Handle;

await new MultiBroadcastNode().Run();
