namespace Nodes.GCounter;

public class CounterNode : Node
{
    private readonly Dictionary<string, long> _externalCounters = new();
    private long _internalCounter;

    private long _messageId;

    private readonly Dictionary<long, string> _pendingExternalCounterUpdates = new();

    private static long Next(ref long messageId) => ++messageId;

    [MessageHandler("add")]
    public void HandleAdd(dynamic msg)
    {
        _internalCounter += (long)msg.Body.Delta;
        Reply(new { Type = "add_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("read")]
    public void HandleRead(dynamic msg)
    {
        Reply(new
        {
            Type = "read_ok", Value = _internalCounter + _externalCounters.Values.Sum(), InReplyTo = msg.Body.MsgId
        });
    }

    [BackgroundProcess(50)]
    public void InitiateExternalCounterUpdate()
    {
        foreach (var node in NodeIds)
            if (node != NodeId)
            {
                _pendingExternalCounterUpdates.Add(Next(ref _messageId), node);
                Send(new
                {
                    Src = NodeId, Dest = "seq-kv", Body = new { Type = "read", Key = node, MsgId = _messageId }
                });
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
        Send(new
        {
            Src = NodeId,
            Dest = "seq-kv",
            Body = new { Type = "write", Key = NodeId, Value = _internalCounter, MsgId = Next(ref _messageId) }
        });
    }
}