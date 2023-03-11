using System.Diagnostics;
using Nodes.Broadcast;

var node = new FaultTolerantBroadcastNode();

// const int stdinPollMillis = 10;
// const int backgroundTaskDelayMillis = 100;


var stopwatch = Stopwatch.StartNew();
var i = 0;
for (string? line = null;; line = Console.In.ReadLine(), i++)
{
    if (line != null)
    {
        node.ProcessMessage(line);
    }

    if (i % 10 == 0)
    {
        Console.Error.WriteLine("performing background tasks");
        node.PerformBackgroundTasks();
    }

    Thread.Sleep(20);
}

// time to build container: 26 seconds
// time to run command: 12 seconds
// time to execute test: 40 seconds
// total time: 78 seconds
// time saved by theoretical fast iteration: 32 seconds