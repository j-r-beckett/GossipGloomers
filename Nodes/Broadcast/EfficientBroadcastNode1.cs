namespace Nodes.Broadcast;

public class EfficientBroadcastNode1 : Node
{
    private readonly HashSet<long> _messages = new();
    private readonly Dictionary<string, HashSet<long>> _updates = new();
    private readonly Dictionary<long, HashSet<long>> _sentUpdates = new();

    private long _messageId;

    private static long Next(ref long messageId) => ++messageId;

    private static bool IsClientBroadcast(dynamic broadcastMsg) => broadcastMsg.Src.ToString().StartsWith("c");

    [BackgroundProcess(1000)]
    public void SendUpdates()
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
                _sentUpdates.Add(_messageId, update);
            }
        }
    }

    [MessageHandler("update")]
    public void HandleUpdate(dynamic msg)
    {
        var update = msg.Body.Update.ToObject<HashSet<long>>();
        _messages.UnionWith(update);
        Reply(new { Type = "update_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("update_ok")]
    public void HandleUpdateOk(dynamic msg)
    {
        if (_sentUpdates.TryGetValue((long)msg.Body.MsgId, out var successfulUpdate) &&
            _updates.TryGetValue((string)msg.Src, out var update))
        {
            update.RemoveWhere(m => successfulUpdate.Contains(m));
        }
    }

    [MessageHandler("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        foreach (var dest in _nodeIds)
        {
            if (dest != _nodeId)
            {
                if (!_updates.TryGetValue(dest, out var update))
                {
                    update = new HashSet<long>();
                    _updates.Add(dest, update);
                }

                update.Add((long)msg.Body.Message);
            }
        }

        _messages.Add((long)msg.Body.Message);
        Reply(new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("broadcast_ok")]
    public void HandleBroadcastOk(dynamic msg)
    {
    }

    [MessageHandler("read")]
    public void HandleRead(dynamic msg)
        => Reply(new
        {
            Type = "read_ok",
            Messages = _messages.AsEnumerable().OrderBy(n => n).ToList(), // sort to make it easier to read output
            InReplyTo = msg.Body.MsgId
        });

    [MessageHandler("topology")]
    public void HandleTopology(dynamic msg) => Reply(new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
}