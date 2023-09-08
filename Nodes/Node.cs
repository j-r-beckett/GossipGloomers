using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;

namespace Nodes;

public abstract class Node
{
    private static readonly TimeSpan _MainLoopDelay = TimeSpan.FromMilliseconds(10);
    private static readonly TimeSpan _ResendDelay = TimeSpan.FromMilliseconds(500);

    private readonly PriorityQueue<BackgroundJob, DateTime> _backgroundJobs = new();

    private readonly ConcurrentQueue<dynamic> _pendingRequests = new();
    private readonly ConcurrentDictionary<long, dynamic> _responses = new();
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
        var lineBuffer = new ConcurrentQueue<string>();
        new Thread(() =>
        {
            var reader = new StreamReader(Console.OpenStandardInput());
            while (true)
            {
                lineBuffer.Enqueue(reader.ReadLine());
            }
        }).Start();

        while (true)
        {
            // Process incoming messages
            while (lineBuffer.TryDequeue(out var line))
            {
                var msg = MessageParser.ParseMessage(line);
                var inReplyTo = (long?) msg.Body.InReplyTo;
                if (inReplyTo.HasValue && _responses.TryGetValue(inReplyTo.Value, out var response) && response == null)
                {
                    _responses[inReplyTo.Value] = msg;
                }
                else
                {
                    var msgType = (string)msg.Body.Type;
                    var handlers = GetType()
                        .GetMethods()
                        .Where(m => m.GetCustomAttributes()
                            .Any(attr => (attr as MessageHandlerAttribute)?.MessageType.ToString() == msgType));
                    foreach (var handler in handlers)
                    {
                        new Thread(() => handler.Invoke(this, new object[] { msg })).Start();
                    }
                }
            }
            
            // Create background jobs for new requests
            while (_pendingRequests.TryDequeue(out var request))
            {
                bool Job(int _)
                {
                    var id = (long)request.Body.MsgId;
                    if (_responses.TryGetValue(id, out var response) && response == null)
                    {
                        WriteMessage(request);
                        return true;
                    }
                    return false;
                }
                
                _backgroundJobs.Enqueue(new BackgroundJob(Job: Job, delay: _ResendDelay, NumInvocations: 0), DateTime.Now);
            }
            
            // Run background jobs
            while (_backgroundJobs.TryPeek(out var backgroundJob,  out var executionTime) && executionTime <= DateTime.Now)
            {
                _backgroundJobs.Dequeue();
                if (backgroundJob.Job.Invoke(backgroundJob.NumInvocations))
                {
                    _backgroundJobs.Enqueue(backgroundJob with { NumInvocations = backgroundJob.NumInvocations + 1},
                        DateTime.Now + backgroundJob.delay);
                }
            }
            
            // Sleep! zz
            Thread.Sleep(_MainLoopDelay);
        }
    }

    public ResponseFuture Send(dynamic msg)
    {
        var id = (long)msg.Body.MsgId;
        _responses.TryAdd(id, null);
        _pendingRequests.Enqueue(msg);
        return new ResponseFuture(this, id);
    }

    protected void Respond(dynamic request, dynamic responseBody) 
        => WriteMessage(new { Src = NodeId, Dest = request.Src, Body = responseBody });
    
    public void WriteMessage(dynamic msg) => Console.WriteLine(JsonConvert.SerializeObject(msg));
    
    [MessageHandler("init")]
    public void HandleInit(dynamic msg)
    {
        NodeId = msg.Body.NodeId;
        NodeIds = msg.Body.NodeIds.ToObject<string[]>();
        Respond(msg, new { Type = "init_ok", InReplyTo = msg.Body.MsgId });
    }

    public class ResponseFuture
    {
        private readonly long _msgId;
        private readonly Node _node;
        private dynamic _response;

        public ResponseFuture(Node node, long msgId)
        {
            (_node, _msgId) = (node, msgId);
        }

        public bool TryGetResponse(out dynamic response)
        {
            response = default;
            if (_response != null || _node._responses.TryRemove(_msgId, out _response))
            {
                response = _response;
                return true;
            }

            return false;
        }
    }
}