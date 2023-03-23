using Nodes;
using Nodes.Kafka;

var node = new SingleKafkaNode();

await NodeRunner.Run(node);