namespace Nodes.Broadcast;

public class EfficientNode4
{
    
}namespace Nodes.Broadcast;

public class EfficientBroadcastNode2 : Node
{
    private readonly HashSet<long> _messages = new();
    private readonly Dictionary<string, HashSet<long>> _updates = new();
    private readonly Dictionary<long, HashSet<long>> _sentUpdates = new();

    private long _messageId;

    private static long Next(ref long messageId) => ++messageId;

    [BackgroundProcess(100)]
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
                _sentUpdates.Add(_messageId, new HashSet<long>(update));
            }
        }
    }

    private void RecordMessage(long message)
    {
        if (!_messages.Contains(message))
        {
            _messages.Add(message);
            foreach (var update in _updates.Values)
            {
                update.Add(message);
            }
        }
    }

    [MessageHandler("update")]
    public void HandleUpdate(dynamic msg)
    {
        var update = msg.Body.Update.ToObject<HashSet<long>>();
        foreach (var message in update)
        {
            RecordMessage(message);
        }

        Reply(new { Type = "update_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("update_ok")]
    public void HandleUpdateOk(dynamic msg)
    {
        var successfulUpdate = _sentUpdates[(long)msg.Body.InReplyTo];
        _updates[(string)msg.Src].RemoveWhere(message => successfulUpdate.Contains(message));
    }

    [MessageHandler("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        RecordMessage((long)msg.Body.Message);
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
            Messages = _messages.AsEnumerable().OrderBy(n => n).ToList(), // sort to make output easier to read
            InReplyTo = msg.Body.MsgId
        });

    [MessageHandler("topology")]
    public void HandleTopology(dynamic msg)
    {
        int nodeNum(string nodeId) => int.Parse(nodeId[1..]);

        foreach (var adjacentNodeId in _nodeIds.Where(n => nodeNum(n) % 2 != nodeNum(_nodeId) % 2))
        {
            _updates.Add(adjacentNodeId, new HashSet<long>());
        }

        Reply(new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
    }
}