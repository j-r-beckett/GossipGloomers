using System.Collections.Concurrent;
using System.Reflection;
using Newtonsoft.Json;
using static Nodes.IO;

namespace Nodes;

public abstract class Node
{
    private static readonly TimeSpan MainLoopDelay = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan ResendDelay = TimeSpan.FromMilliseconds(500);

    private readonly MessageProcessor _messageProcessor = new();
    private readonly PriorityQueue<BackgroundJob, DateTime> _backgroundJobs = new();
    private readonly ConcurrentQueue<(dynamic, MessageProcessor.ResponseFuture)> _unprocessedRequests = new();

    private readonly object _msgIdLock = new ();
    private long _msgId;
    
    public Node()
    {
        var backgroundJobs = GetType()
            .GetMethods()
            .Select(m => (method: m, attr: m.GetCustomAttribute<BackgroundProcessAttribute>()))
            .Where(t => t.attr != null)
            .Select(t => new BackgroundJob(
                Job: _ =>
                {
                    if (NodeId != null && NodeIds != null)  // only execute background methods after the node has finished initializing
                    {
                        t.method.Invoke(this, Array.Empty<object>());
                    }
                    return true;  // background jobs are always renewed
                }, 
                delay: TimeSpan.FromMilliseconds(t.attr.IntervalMillis),
                NumInvocations: 0));
        
        _backgroundJobs.EnqueueRange(backgroundJobs.Select(job => (job, DateTime.Now)));
    }
    
    protected string? NodeId { get; set; }
    protected string[]? NodeIds { get; set; }

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
            
            // Process all lines in stdin buffer
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
                _backgroundJobs.Enqueue(
                    new BackgroundJob(
                        Job: n => 
                        {
                            var (msg, future) = request;
                            if (n > 0) Console.Error.WriteLine($"resending {JsonConvert.SerializeObject(msg)}");
                            var hasReceivedResponse = future.TryGetResponse(out _);
                            if (!hasReceivedResponse) WriteMessage(msg);
                            return !hasReceivedResponse;
                        }, 
                        delay: ResendDelay, 
                        NumInvocations: 0), 
                    DateTime.Now + ResendDelay);
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
            var nextRunTime = startTime + MainLoopDelay;
            TimeSpan Max(TimeSpan t1, TimeSpan t2) => new (Math.Max(t1.Ticks, t2.Ticks));
            // Console.Error.WriteLine($"elapsed time: {(DateTime.Now - startTime).TotalMilliseconds} ms, " +
            //                         $"sleeping for: {Max(nextRunTime - DateTime.Now, TimeSpan.Zero).TotalMilliseconds} ms");
            Thread.Sleep(Max(nextRunTime - DateTime.Now, TimeSpan.Zero));
        }
    }

    public MessageProcessor.ResponseFuture SendRequest(dynamic msg)
    {
        WriteMessage(msg);
        var future = _messageProcessor.ProcessRequest(msg);
        _unprocessedRequests.Enqueue((msg, future));
        return future;
    }

    protected long NextMsgId()
    {
        lock (_msgIdLock)
        {
            return _msgId++;
        }
    }
    
    private record BackgroundJob(Func<int, bool> Job, TimeSpan delay, int NumInvocations);
}