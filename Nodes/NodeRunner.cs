using System.Reflection;

namespace Nodes;

public static class NodeRunner
{
    private static async Task Schedule(long intervalMillis, Action action)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMillis));
        while (await timer.WaitForNextTickAsync()) action();
    }

    public static async Task Run(Node node, long stdinPollIntervalMillis = 20)
    {
        var backgroundTasks = node.GetType()
            .GetMethods()
            .Select(m => (method: m, attr: m.GetCustomAttribute<BackgroundProcessAttribute>()))
            .Where(t => t.attr != null)
            .ToDictionary(t => t.method, t => TimeSpan.FromMilliseconds(t.attr.IntervalMillis));

        var lastInvoked = new Dictionary<MethodInfo, DateTime>();

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