using System.Reflection;

namespace Nodes;

public static class NodeRunner
{
    private static Random _random = new();
    private static object nodeLock = new();

    private static async Task Schedule(long intervalMillis, Action action)
    {
        Console.Error.WriteLine("invoking Schedule");
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMillis));
        var lastRanTimestamp = DateTime.Now - TimeSpan.FromMilliseconds(50);
        while (await timer.WaitForNextTickAsync())
        {
            // lock (nodeLock)
            // {
            Console.Error.WriteLine(
                $"elapsed millis since last invoked: {(DateTime.Now - lastRanTimestamp).TotalMilliseconds}");
            lastRanTimestamp = DateTime.Now;
            action();
            Console.Error.WriteLine("action complete");
            // }
        }

        Console.Error.WriteLine("exiting");
    }

    public static async Task Run(Node node, long stdinPollIntervalMillis = 20)
    {
        // var consumeMessageTask = Schedule(stdinPollIntervalMillis, () =>
        // {
        //     var line = Console.In.ReadLine();
        //     if (line != null)
        //     {
        //         node.ProcessMessage(line);
        //     }
        // });
        //
        // var backgroundTasks = node.GetType()
        //     .GetMethods()
        //     .Select(m => (method: m, attr: m.GetCustomAttribute<BackgroundProcessAttribute>()))
        //     .Where(t => t.attr != null)
        //     .Select(t => Schedule(t.attr.IntervalMillis, () =>
        //     {
        //         Console.Error.WriteLine($"invoking action on method {t.method}");
        //         // t.method.Invoke(node, Array.Empty<object>());
        //     }))
        //     .ToList();

        var backgroundTasks = node.GetType()
            .GetMethods()
            .Select(m => (method: m, attr: m.GetCustomAttribute<BackgroundProcessAttribute>()))
            .Where(t => t.attr != null)
            .ToDictionary(t => t.method, t => TimeSpan.FromMilliseconds(t.attr.IntervalMillis));

        var lastInvoked = new Dictionary<MethodInfo, DateTime>();

        // backgroundTasks.Add(consumeMessageTask);
        //
        // await Task.WhenAll(backgroundTasks);
        while (true)
        {
            var line = Console.In.ReadLine();
            if (line != null) node.ProcessMessage(line);

            foreach (var (method, delay) in backgroundTasks)
            {
                var lastInvokedAt = lastInvoked.GetValueOrDefault(method, DateTime.MinValue);
                if (lastInvokedAt + delay < DateTime.Now)
                {
                    method.Invoke(node, Array.Empty<object>());
                    lastInvoked[method] = DateTime.Now;
                }
            }
        }
    }
}