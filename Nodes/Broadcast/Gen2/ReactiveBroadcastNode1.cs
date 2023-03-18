namespace Nodes.Broadcast.Gen2;

public class ReactiveBroadcastNode1 : Node
{
    private readonly HashSet<long> _messages = new();
    private readonly Dictionary<string, HashSet<long>> _updates = new();
    private readonly Dictionary<long, HashSet<long>> _sentUpdates = new();
    private readonly System.Timers.Timer _timer = new();
    private const int _UpdateIntervalMillis = 200;

    public ReactiveBroadcastNode1()
    {
        _timer.Elapsed += (_, _) => SendUpdates();
        _timer.Interval = _UpdateIntervalMillis;
        _timer.Enabled = true;
    }

    private long _messageId;

    private static long Next(ref long messageId) => ++messageId;

    private void SendUpdates()
    {
        foreach (var (dest, update) in _updates)
        {
            if (update.Any())
            {
                Send(new
                {
                    Src = _nodeId,
                    Dest = dest,
                    Body = new { Type = "update", Update = update, MsgId = Next(ref _messageId) }
                });
                _sentUpdates.Add(_messageId, new HashSet<long>(update));
            }
        }
    }

    [MessageHandler("update")]
    public void HandleUpdate(dynamic msg)
    {
        HashSet<long> update = msg.Body.Update.ToObject<HashSet<long>>();
        foreach (var message in update)
        {
            _messages.Add(message);
        }
        Reply(new { Type = "update_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("update_ok")]
    public void HandleUpdateOk(dynamic msg)
    {
        var inReplyTo = (long)msg.Body.InReplyTo;
        if (_sentUpdates.TryGetValue(inReplyTo, out var successfulUpdate))
        {
            _updates[(string)msg.Src].RemoveWhere(message => successfulUpdate.Contains(message));
            _sentUpdates.Remove(inReplyTo);
        }
    }

    [MessageHandler("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        var message = (long)msg.Body.Message;
        _messages.Add(message);
        foreach (var update in _updates.Values)
        {
            update.Add(message);
        }
        Reply(new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("read")]
    public void HandleRead(dynamic msg)
        => Reply(new
        {
            Type = "read_ok",
            Messages = _messages.AsEnumerable().OrderBy(n => n).ToList(), // sort to make output easier to read
            InReplyTo = msg.Body.MsgId
        });

    [MessageHandler("topology")]
    public void HandleTopology(dynamic msg)
    {
        foreach (var node in _nodeIds)
        {
            if (node != _nodeId)
            {
                _updates.Add(node, new HashSet<long>());
            }
        }

        Reply(new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
    }
}