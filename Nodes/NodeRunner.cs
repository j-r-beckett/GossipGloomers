using System.Reflection;

namespace Nodes;

public static class NodeRunner
{
    private static async Task Schedule(long intervalMillis, object lockObj, Action action)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMillis));
        while (await timer.WaitForNextTickAsync())
        {
            lock (lockObj)
            {
                action();
            }
        }
    }

    public static async Task Run(Node node, long stdinPollIntervalMillis = 20)
    {
        var consumeMessageTask = Schedule(stdinPollIntervalMillis, node, () =>
        {
            var line = Console.In.ReadLine();
            if (line != null)
            {
                node.ProcessMessage(line);
            }
        });

        var backgroundTasks = node.GetType()
            .GetMethods()
            .Select(m => (method: m, attr: m.GetCustomAttribute<BackgroundProcessAttribute>()))
            .Where(t => t.attr != null)
            .Select(t => Schedule(t.attr.IntervalMillis, node, () => t.method.Invoke(node, Array.Empty<object>())))
            .ToList();
        
        backgroundTasks.Add(consumeMessageTask);
        
        await Task.WhenAll(backgroundTasks);
    }
}