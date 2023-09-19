using System.Collections.Concurrent;
using System.Reflection;
using Newtonsoft.Json;

namespace Nodes;

public abstract class Node
{
    private static readonly TimeSpan _MainLoopDelay = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan _ResendDelay = TimeSpan.FromMilliseconds(500);

    private readonly PriorityQueue<BackgroundJob, DateTime> _backgroundJobs = new();

    private readonly MessageProcessor _messageProcessor = new();
    private readonly ConcurrentQueue<(dynamic, MessageProcessor.ResponseFuture)> _unprocessedRequests = new();
    
    public string? NodeId;
    public string[] NodeIds;

    private record BackgroundJob(Func<int, bool> Job, TimeSpan delay, int NumInvocations);

    public Node()
    {
        var backgroundTasks = GetType()
            .GetMethods()
            .Select(m => (method: m, attr: m.GetCustomAttribute<BackgroundProcessAttribute>()))
            .Where(t => t.attr != null)
            .Select(t => (t.method, delay: TimeSpan.FromMilliseconds(t.attr.IntervalMillis)));

        foreach (var (method, delay) in backgroundTasks)
        {
            var backgroundJob = new BackgroundJob(
                Job: _ =>
                {
                    if (NodeId != null && NodeIds != null)  // only run background methods after the node has finished initializing
                    {
                        method.Invoke(this, Array.Empty<object>());
                    }
                    return true;
                }, 
                delay: delay,
                NumInvocations: 0);
            _backgroundJobs.Enqueue(backgroundJob, DateTime.Now);
        }
    }

    public void Run()
    {
        // Spin off a thread to read from stdin into a buffer
        var lineBuffer = new ConcurrentQueue<string>();
        new Thread(() =>
        {
            var reader = new StreamReader(Console.OpenStandardInput());
            while (true)
            {
                lineBuffer.Enqueue(reader.ReadLine());
            }
        }).Start();

        // Main loop
        while (true)
        {
            var startTime = DateTime.Now;
            
            // Process all lines in buffer
            while (lineBuffer.TryDequeue(out var line))
            {
                var msg = MessageParser.ParseMessage(line);
                if (!_messageProcessor.TryProcessResponse(msg))  // Updates response future if message is a response
                {
                    // Message is NOT a response to another message, so we pass it to a message handler
                    var msgType = (string)msg.Body.Type;
                    var handlers = GetType()
                        .GetMethods()
                        .Where(m => m.GetCustomAttributes()
                            .Any(attr => (attr as MessageHandlerAttribute)?.MessageType.ToString() == msgType));
                    // TODO: throw exception if more than one handler found?
                    foreach (var handler in handlers)
                    {
                        // Spin off a new thread to handle each request
                        new Thread(() => handler.Invoke(this, new object[] { msg })).Start();
                    }
                }
            }
            
            // Create background jobs for unprocessed requests
            while (_unprocessedRequests.TryDequeue(out var request))
            {
                bool Job(int n)
                {
                    if (n > 0)
                        Console.Error.WriteLine($"sending {JsonConvert.SerializeObject(request.Item1)} {n}");
                    
                    var (msg, future) = request;
                    var hasReceivedResponse = future.TryGetResponse(out _);
                    if (!hasReceivedResponse) WriteMessage(msg);
                    return !hasReceivedResponse;
                }

                _backgroundJobs.Enqueue(new BackgroundJob(Job: Job, delay: _ResendDelay, NumInvocations: 0), DateTime.Now);
            }
            
            // Run background jobs in main thread
            while (_backgroundJobs.TryPeek(out var backgroundJob, out var executionTime) && executionTime <= DateTime.Now)
            {
                _backgroundJobs.Dequeue();
                // Console.Error.WriteLine($"executing job with time delta {(DateTime.Now - executionTime).TotalMilliseconds} ms");
                if (backgroundJob.Job.Invoke(backgroundJob.NumInvocations))
                {
                    _backgroundJobs.Enqueue(backgroundJob with { NumInvocations = backgroundJob.NumInvocations + 1},
                        DateTime.Now + backgroundJob.delay);
                }
            }
            
            // Sleep! zz
            var nextRunTime = startTime + _MainLoopDelay;
            TimeSpan Max(TimeSpan t1, TimeSpan t2) => new (Math.Max(t1.Ticks, t2.Ticks));
            Thread.Sleep(Max(DateTime.Now - nextRunTime, TimeSpan.Zero));
        }
    }

    public MessageProcessor.ResponseFuture SendRequest(dynamic msg)
    {
        var future = _messageProcessor.ProcessRequest(msg);
        _unprocessedRequests.Enqueue((msg, future));
        return future;
    }

    protected void WriteResponse(dynamic request, dynamic responseBody) 
        => WriteMessage(new { Src = NodeId, Dest = request.Src, Body = responseBody });
    
    public void WriteMessage(dynamic msg) => Console.WriteLine(JsonConvert.SerializeObject(msg));
    
    protected static int Next(ref int messageId) => ++messageId;
    protected int _messageId = -1;
}