namespace Nodes.Broadcast;

public class EfficientBroadcastNode2 : Node
{
    private readonly HashSet<long> _messages = new();
    private Dictionary<string, HashSet<long>> _unsentUpdates = new();

    private int _messageId = -1;

    [BackgroundProcess(1250)]
    public void SendUpdates()
    {
        foreach (var nodeId in NodeIds)
        {
            var update = new HashSet<long>(_unsentUpdates[nodeId]);
            Send(new
            {
                Src = NodeId, Dest = nodeId, Body = new { Type = "update", Update = update, MsgId = ++_messageId }
            }).OnReponse("update_ok", _messageId, msg =>
            {
                _unsentUpdates[nodeId].RemoveWhere(message => update.Contains(message));
            });
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

        Reply(msg, new { Type = "update_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        var message = (int)msg.Body.Message;
        _messages.Add(message);
        foreach (var nodeId in NodeIds)
        {
            _unsentUpdates[nodeId].Add(message);
        }
        Reply(msg, new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("read")]
    public void HandleRead(dynamic msg)
    {
        Reply(msg, new
        {
            Type = "read_ok",
            Messages = _messages.AsEnumerable().OrderBy(n => n).ToList(), // sort to make it easier to read output
            InReplyTo = msg.Body.MsgId
        });
    }

    [MessageHandler("topology")]
    public void HandleTopology(dynamic msg)
    {
        foreach (var nodeId in NodeIds)
        {
            _unsentUpdates.Add(nodeId, new HashSet<long>());
        }
        Reply(msg, new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
    }
}