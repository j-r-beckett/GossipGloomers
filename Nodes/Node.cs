using System.Collections.Concurrent;
using System.Reflection;
using Newtonsoft.Json;

namespace Nodes;

public abstract class Node
{
    private dynamic? _context;
    public string? NodeId;
    public string[] NodeIds;
    public HashSet<long> PendingReplyIds = new();
    private HashSet<string> _responseTypes = new();
    private Dictionary<long, Action<dynamic>> _responseHandlers = new();

    private PriorityQueue<Job, DateTime> _jobs = new();
    private const int _stdinPollIntervalMillis = 20;

    private ConcurrentQueue<string> _unprocessedLines = new();
    private Thread _lineReader;

    public Node()
    {
        _lineReader = new(ReadLines);
        _lineReader.Start(_unprocessedLines);
        
        Schedule(new Job
        {
            Callback = _ =>
            {
                while (_unprocessedLines.TryDequeue(out var line))
                {
                    ProcessMessage(line);
                }
                return true;
            },
            Delay = TimeSpan.FromMilliseconds(_stdinPollIntervalMillis)
        });
        
        var backgroundTasks = GetType()
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
                    if (NodeId != null)
                    {
                        method.Invoke(this, Array.Empty<object>());
                    }
                    return true;
                },
                Delay = delay
            });
        }
    }

    public void AddResponseHandler(string msgType, long inReplyTo, Action<dynamic> handler)
    {
        _responseTypes.Add(msgType);
        _responseHandlers.Add(inReplyTo, handler);
    }
    
    public void Schedule(Job job) => _jobs.Enqueue(job, DateTime.Now + job.Delay);

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

    private static void ReadLines(object obj)
    {
        var lines = (ConcurrentQueue<string>)obj;
        while (true)
        {
            var line = Console.ReadLine();
            if (line != null) lines.Enqueue(line);
        }
    }

    public CallbackRegistrar Send(dynamic msg)
    {
        PendingReplyIds.Add((long)msg.Body.MsgId);
        var msgJson = JsonConvert.SerializeObject(msg);
        // Log($"sending msg {msgJson}");
        Console.WriteLine(msgJson);
        return new CallbackRegistrar(this, msg);
    }

    public void ProcessMessage(string msgStr)
    {
        // Console.Error.WriteLine($"processing msg {msgStr}");
        var msg = MessageParser.ParseMessage(msgStr);
        var msgType = (string)msg.Body.Type;
        if (_responseTypes.Contains(msgType))
        {
            var inReplyTo = (long)msg.Body.InReplyTo;
            if (_responseHandlers.TryGetValue(inReplyTo, out var handler))
            {
                handler.Invoke(msg);
                _responseHandlers.Remove(inReplyTo);                
            }
        }
        else
        {
            var handlers = GetType()
                .GetMethods()
                .Where(m => m.GetCustomAttributes()
                    .Any(attr => (attr as MessageHandlerAttribute)?.MessageType.ToString() == msgType));
            foreach (var handler in handlers)
            {
                _context = msg;
                handler.Invoke(this, new object[] { msg });
                _context = null;
            }
        }
    }

    [MessageHandler("init")]
    public void HandleInit(dynamic msg)
    {
        NodeId = msg.Body.NodeId;
        NodeIds = msg.Body.NodeIds.ToObject<string[]>();
        Reply(new { Type = "init_ok", InReplyTo = msg.Body.MsgId });
    }

    protected void Reply(dynamic payload)
    {
        var msg = new { Src = NodeId, Dest = _context.Src, Body = payload };
        var msgJson = JsonConvert.SerializeObject(msg);
        // Log($"sending msg {msgJson}");
        Console.WriteLine(msgJson);
    }
}

public record Job
{
    // Callback(n) => bool, n is number of invocations (n=0 on first invocation), rerun callback after interval
    // if callback returns true
    public required Func<int, bool> Callback { get; init; }
    public required TimeSpan Delay { get; init; }
    public int Invocations { get; init; } = 0;
}