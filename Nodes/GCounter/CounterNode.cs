namespace Nodes.GCounter;

public class CounterNode : InitNode
{
    private readonly Dictionary<string, long> _externalCounters = new();
    private long _internalCounter;

    private readonly Dictionary<long, string> _pendingExternalCounterUpdates = new();

    [MessageHandler("add")]
    public void HandleAdd(dynamic msg)
    {
        _internalCounter += (long)msg.Body.Delta;
        WriteResponse(msg, new { Type = "add_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("read")]
    public void HandleRead(dynamic msg)
    {
        WriteResponse(msg, new
        {
            Type = "read_ok", Value = _internalCounter + _externalCounters.Values.Sum(), InReplyTo = msg.Body.MsgId
        });
    }

    [BackgroundProcess(100)]
    public void InitiateExternalCounterUpdate()
    {
        foreach (var node in NodeIds)
        {
            if (node != NodeId)
            {
                var msgId = NextMsgId();
                _pendingExternalCounterUpdates.Add(msgId, node);
                WriteMessage(new
                {
                    Src = NodeId, Dest = "seq-kv", Body = new { Type = "read", Key = node, MsgId = msgId }
                });
            }
        }
    }

    [MessageHandler("read_ok")]
    public void HandleExternalCounterUpdate(dynamic msg)
    {
        var inReplyTo = (long)msg.Body.InReplyTo;
        if (_pendingExternalCounterUpdates.TryGetValue(inReplyTo, out var node))
        {
            _pendingExternalCounterUpdates.Remove(inReplyTo);
            _externalCounters[node] = (long)msg.Body.Value;
        }
    }

    [BackgroundProcess(100)]
    public void PropagateInternalCounter()
    {
        WriteMessage(new
        {
            Src = NodeId,
            Dest = "seq-kv",
            Body = new { Type = "write", Key = NodeId, Value = _internalCounter, MsgId = NextMsgId() }
        });
    }
}