namespace Nodes.Broadcast.Gen3;

public class EfficientBroadcastNode : Node
{
    private readonly HashSet<long> _messages = new();

    private int _messageId;
    private string[] _neighbors = { };

    private readonly Dictionary<long, HashSet<long>> _sentUpdates = new();

    // private Dictionary<long, (dynamic, DateTime)> _pendingUpdates = new();
    private readonly Dictionary<string, HashSet<long>> _unackedUpdates = new();

    private static int Next(ref int messageId)
    {
        return ++messageId;
    }

    private static bool IsClientBroadcast(dynamic broadcastMsg)
    {
        return broadcastMsg.Src.ToString().StartsWith("c");
    }

    [BackgroundProcess(200)]
    public void SendUnackedUpdates()
    {
        foreach (var (node, update) in _unackedUpdates)
            if (update.Any())
            {
                Send(new
                {
                    Src = NodeId,
                    Dest = node,
                    Body = new { Type = "update", Update = update, MsgId = Next(ref _messageId) }
                });
                _sentUpdates.Add(_messageId, update);
            }
    }


    [MessageHandler("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        var message = (long)msg.Body.Message;
        _messages.Add(message);

        if (IsClientBroadcast(msg))
        {
            foreach (var nodeId in NodeIds)
                if (nodeId != NodeId)
                    Send(new
                    {
                        Src = NodeId,
                        Dest = nodeId,
                        Body = new { Type = "broadcast", Message = message, MsgId = Next(ref _messageId) }
                    });

            foreach (var neighbor in _neighbors) _unackedUpdates[neighbor].Add(message);

            Reply(new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
        }
    }

    [MessageHandler("update")]
    public void HandleUpdate(dynamic msg)
    {
        HashSet<long> update = msg.Body.Update.ToObject<HashSet<long>>();
        var newMessages = update.Where(m => !_messages.Contains(m));
        foreach (var message in newMessages)
        {
            _messages.Add(message);
            foreach (var neighbor in _neighbors) _unackedUpdates[neighbor].Add(message);
        }

        if (newMessages.Any()) SendUnackedUpdates();

        Reply(new { Type = "update_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("update_ok")]
    public void HandleUpdateOk(dynamic msg)
    {
        var inReplyTo = (long)msg.Body.InReplyTo;
        if (_sentUpdates.ContainsKey(inReplyTo))
        {
            var update = _sentUpdates[inReplyTo];
            _unackedUpdates[(string)msg.Src].RemoveWhere(m => update.Contains(m));
            _sentUpdates.Remove(inReplyTo);
        }
    }

    [MessageHandler("read")]
    public void HandleRead(dynamic msg)
    {
        Reply(new
        {
            Type = "read_ok",
            Messages = _messages.AsEnumerable().OrderBy(n => n).ToList(), // sort to make it easier to read output
            InReplyTo = msg.Body.MsgId
        });
    }

    [MessageHandler("topology")]
    public void HandleTopology(dynamic msg)
    {
        var topology = msg.Body.Topology.ToObject<Dictionary<string, string[]>>();
        _neighbors = topology[NodeId.ToUpper()];

        foreach (var neighbor in _neighbors) _unackedUpdates.Add(neighbor, new HashSet<long>());

        Reply(new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
    }
}