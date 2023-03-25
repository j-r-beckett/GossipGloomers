using System.Collections.Concurrent;
using System.Reflection;

namespace Nodes;

public class NodeRunner
{
    private Node _node;
    private PriorityQueue<Job, DateTime> _jobs = new();
    private const int _stdinPollIntervalMillis = 20;

    private ConcurrentQueue<string> _unprocessedLines = new();
    private Thread _lineReader;

    public NodeRunner(Node node)
    {
        _node = node;

        _lineReader = new(ReadLines);
        _lineReader.Start(_unprocessedLines);
        
        Schedule(new Job
        {
            Callback = _ =>
            {
                while (_unprocessedLines.TryDequeue(out var line))
                {
                    _node.ProcessMessage(line);
                }
                return true;
            },
            Delay = TimeSpan.FromMilliseconds(_stdinPollIntervalMillis)
        });
        
        var backgroundTasks = _node.GetType()
            .GetMethods()
            .Select(m => (method: m, attr: m.GetCustomAttribute<BackgroundProcessAttribute>()))
            .Where(t => t.attr != null)
            .ToDictionary(t => t.method, t => TimeSpan.FromMilliseconds(t.attr.IntervalMillis));

        foreach (var (method, delay) in backgroundTasks)
        {
            Schedule(new Job
            {
                Callback = _ =>
                {
                    if (_node.NodeId != null)
                    {
                        method.Invoke(_node, Array.Empty<object>());
                    }
                    return true;
                },
                Delay = delay
            });
        }
    }
    
    private void Schedule(Job job) => _jobs.Enqueue(job, DateTime.Now + job.Delay);

    public Task Run()
    {
        while (true)
        {
            if (_jobs.TryPeek(out var job, out var priority) && priority < DateTime.Now)
            {
                _jobs.Dequeue();
                var repeat = job.Callback.Invoke(job.Invocations);
                if (repeat)
                {
                    Schedule(job with { Invocations = job.Invocations + 1 });
                }
            }
            
            Thread.Sleep(_stdinPollIntervalMillis);
        }
    }
    
    private record Job
    {
        // Callback(n) => bool, n is number of invocations (n=0 on first invocation), rerun callback after interval
        // if callback returns true
        public required Func<int, bool> Callback { get; init; }
        public required TimeSpan Delay { get; init; }
        public int Invocations { get; init; } = 0;
    }

    private static void ReadLines(object obj)
    {
        var lines = (ConcurrentQueue<string>)obj;
        while (true)
        {
            var line = Console.ReadLine();
            if (line != null) lines.Enqueue(line);
        }
    }
}