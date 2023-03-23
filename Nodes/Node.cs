using System.Reflection;

namespace Nodes;

public abstract class Node
{
    private dynamic? _context;
    protected string? _nodeId;
    protected string[] _nodeIds;

    public async Task Run(long stdinPollIntervalMillis = 20)
    {
        var backgroundTasks = GetType()
            .GetMethods()
            .Select(m => (method: m, attr: m.GetCustomAttribute<BackgroundProcessAttribute>()))
            .Where(t => t.attr != null)
            .ToDictionary(t => t.method, t => TimeSpan.FromMilliseconds(t.attr.IntervalMillis));

        var lastInvoked = new Dictionary<MethodInfo, DateTime>();

        while (true)
        {
            var line = Console.In.ReadLine();
            if (line != null) ProcessMessage(line);

            foreach (var (method, delay) in backgroundTasks)
            {
                var lastInvokedAt = lastInvoked.GetValueOrDefault(method, DateTime.MinValue);
                if (lastInvokedAt + delay < DateTime.Now)
                {
                    method.Invoke(this, Array.Empty<object>());
                    lastInvoked[method] = DateTime.Now;
                }
            }
        }
    }

    private static async Task Schedule(long intervalMillis, Action action)
    {
        var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMillis));
        while (await timer.WaitForNextTickAsync()) action();
    }

    private void ProcessMessage(string msgStr)
    {
        Console.Error.WriteLine($"processing msg {msgStr}");
        var msg = MessageParser.ParseMessage(msgStr);
        var msgType = (string)msg.Body.Type;
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

    [MessageHandler("init")]
    public void HandleInit(dynamic msg)
    {
        _nodeId = msg.Body.NodeId;
        _nodeIds = msg.Body.NodeIds.ToObject<string[]>();
        Reply(new { Type = "init_ok", InReplyTo = msg.Body.MsgId });
    }

    protected void Reply(dynamic payload)
    {
        MaelstromUtils.Send(new { Src = _nodeId, Dest = _context.Src, Body = payload });
    }
}