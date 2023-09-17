using System.Collections.Immutable;
using Newtonsoft.Json;

namespace Nodes.Broadcast;

public class EfficientBroadcastNode : InitNode
{
    private ImmutableHashSet<long> _messages = ImmutableHashSet<long>.Empty;
    
    // {nodeId -> {msgId -> message}}
    private ImmutableDictionary<string, ImmutableDictionary<long, long>> _unackedUpdates 
        = ImmutableDictionary<string, ImmutableDictionary<long, long>>.Empty; 
    
    // nodeId -> MsgId}
    private ImmutableDictionary<string, long> _unsentUpdateAcks 
        = ImmutableDictionary<string, long>.Empty;

    [BackgroundProcess(2000)]
    public void SendUpdateIds()
    {
        foreach (var (node, id) in _unsentUpdateAcks)
        {
            WriteMessage(new
            {
                Src = NodeId,
                Dest = node,
                Body = new { Type = "update_ack", UpdateId = id, MsgId = Next(ref _messageId) }
            });
        }

        // We may throw away some acks here that weren't processed above. But it's fine! The updates will just be 
        // resent and new acks created
        _unsentUpdateAcks = ImmutableDictionary<string, long>.Empty;
    }
    
    [MessageHandler("update")]
    public void HandleUpdate(dynamic msg)
    {
        HashSet<long> update = msg.Body.Update.ToObject<HashSet<long>>();
        ImmutableInterlocked.Update(ref _messages, messages => messages.Union(update));
        ImmutableInterlocked.TryAdd(ref _unsentUpdateAcks, (string)msg.Src, (long)msg.Body.MsgId);
    }

    [MessageHandler("update_ack")]
    public void HandleUpdateAck(dynamic msg)
    {
        var srcNodeId = (string)msg.Src;
        ImmutableDictionary<long, long> RemoveAckedUpdates(ImmutableDictionary<long, long> updates)
            => updates.Where(p => p.Key > (long)msg.Body.UpdateId).ToImmutableDictionary();
        while (!ImmutableInterlocked.TryUpdate(ref _unackedUpdates, srcNodeId,
                   RemoveAckedUpdates(_unackedUpdates[srcNodeId]), _unackedUpdates[srcNodeId])) ;
    }

    [MessageHandler("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        var message = (long)msg.Body.Message;
        ImmutableInterlocked.Update(ref _messages, messages => messages.Add(message));
        foreach (var nodeId in NodeIds)
        {
            if (nodeId != NodeId)
            {
                var msgId = (long)Next(ref _messageId);
                ImmutableInterlocked.TryAdd(ref _unackedUpdates, nodeId, ImmutableDictionary<long, long>.Empty);
                while (!ImmutableInterlocked.TryUpdate(ref _unackedUpdates, nodeId,
                           _unackedUpdates[nodeId].Add(msgId, message), _unackedUpdates[nodeId])) ;
                var updateMsg = new
                {
                    Src = NodeId,
                    Dest = nodeId,
                    Body = new { Type = "update", Update = _unackedUpdates[nodeId].Values, MsgId = msgId }
                };
                // Console.Error.WriteLine($"sending update {JsonConvert.SerializeObject(updateMsg)}");
                WriteMessage(updateMsg);
            }
        }
        WriteResponse(msg, new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
    }
    
    [MessageHandler("read")]
    public void HandleRead(dynamic msg)
    {
        WriteResponse(msg, new
        {
            Type = "read_ok",
            Messages = _messages.AsEnumerable().OrderBy(n => n).ToList(), // sort to make it easier to read output
            InReplyTo = msg.Body.MsgId
        });
    }

    [MessageHandler("topology")]
    public void HandleTopology(dynamic msg)
    {
        WriteResponse(msg, new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
    }
}