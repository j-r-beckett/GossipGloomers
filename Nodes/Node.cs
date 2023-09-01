using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Newtonsoft.Json;

namespace Nodes;

public abstract class Node
{
    public string? NodeId;
    public string[] NodeIds;

    private Dictionary<long, dynamic> _responses = new();

    private PriorityQueue<MethodInfo, DateTime> _backgroundJobs = new();
    private static readonly TimeSpan _eventLoopDelay = TimeSpan.FromMilliseconds(20);

    public Node()
    {
        var backgroundTasks = GetType()
            .GetMethods()
            .Select(m => (method: m, attr: m.GetCustomAttribute<BackgroundProcessAttribute>()))
            .Where(t => t.attr != null)
            .Select(t => (t.method, delay: TimeSpan.FromMilliseconds(t.attr.IntervalMillis)));
        
        _backgroundJobs.EnqueueRange(backgroundTasks.Select(t => (t.method, DateTime.Now + t.delay)));
    }

    public async Task Run()
    {
        var lineBuffer = new ConcurrentQueue<string>();
        Task.Run(async () =>
        {
            var reader = new StreamReader(Console.OpenStandardInput());
            while (true)
            {
                lineBuffer.Enqueue(await reader.ReadLineAsync());
            }
        });
        
        while (true)
        {
            if (lineBuffer.TryDequeue(out var line))
            {
                ProcessMessage(line);
            }
        }
    }

    public async Task ProcessMessage(string msgStr)
    {
        var msg = MessageParser.ParseMessage(msgStr);
        var msgType = (string)msg.Body.Type;
        try
        {
            var inReplyTo = (long)msg.Body.InReplyTo;
            if (_responses.TryGetValue(inReplyTo, out var v) && v == null)
            {
                _responses[inReplyTo] = msg;
            }
        }
        catch (Exception)
        {
            var handlers = GetType()
                .GetMethods()
                .Where(m => m.GetCustomAttributes()
                    .Any(attr => (attr as MessageHandlerAttribute)?.MessageType.ToString() == msgType));
            foreach (var handler in handlers)
            {
                handler.Invoke(this, new object[] { msg });
            }
        }
    }

    [MessageHandler("init")]
    public async Task HandleInit(dynamic msg)
    {
        NodeId = msg.Body.NodeId;
        NodeIds = msg.Body.NodeIds.ToObject<string[]>();
        Reply(msg, new { Type = "init_ok", InReplyTo = msg.Body.MsgId });
    }
    
    public CallbackRegistrar Send(dynamic msg)
    {
        // PendingReplyIds.Add((long)msg.Body.MsgId);
        var msgJson = JsonConvert.SerializeObject(msg);
        Console.WriteLine(msgJson);
        return new CallbackRegistrar(this, msg);
    }

    public async Task SendAndWait(dynamic msg)
    {
        var id = (long)msg.Body.MsgId;
        var msgJson = JsonConvert.SerializeObject(msg);
        Console.WriteLine(msgJson);
        
        var resendDelay = TimeSpan.FromMilliseconds(500);
        var pollDelay = _eventLoopDelay / 2;
        var lastSentTime = DateTimeOffset.Now;
        _responses.Add(id, null);
        while (_responses[id] == null)
        {
            if (DateTime.Now - lastSentTime > resendDelay)
            {
                Console.WriteLine(msgJson);
                lastSentTime = DateTimeOffset.Now;
            }

            await Task.Delay(pollDelay);
        }

        var response = _responses[id];
        _responses.Remove(id);
    }

    protected void Reply(dynamic request, dynamic responseBody)
    {
        var msg = new { Src = NodeId, Dest = request.Src, Body = responseBody };
        var msgJson = JsonConvert.SerializeObject(msg);
        Console.WriteLine(msgJson);
    }
}
