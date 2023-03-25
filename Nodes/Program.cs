using Nodes;
using Nodes.Broadcast;
using Nodes.Broadcast.Gen3;
using Nodes.GCounter;
using Nodes.Kafka;

await new NodeRunner(new MultiBroadcastNode()).Run();
