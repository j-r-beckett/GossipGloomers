using System.Collections.Immutable;
using Newtonsoft.Json;
using static Nodes.IO;

namespace Nodes.Broadcast;

public class EfficientBroadcastNode : Node
{
    private ImmutableHashSet<long> _messages = ImmutableHashSet<long>.Empty;
    
    // {nodeId -> { message }}
    private ImmutableDictionary<string, ImmutableHashSet<long>> _pendingUpdates 
        = ImmutableDictionary<string, ImmutableHashSet<long>>.Empty;

    private int NumGroups => 5;
    private int Group(string nodeId) => int.Parse(nodeId[1..]) % NumGroups;
    private string Leader(int group) => $"n{group}";
    private IEnumerable<string> Nodes(int group) => NodeIds.Where(n => group == Group(n));
    private IEnumerable<string> Leaders() => NodeIds.Where(IsLeader);
    private bool IsLeader(string nodeId) => nodeId == Leader(Group(nodeId));
    private bool IsFromClient(dynamic msg) => msg.Src.ToString().StartsWith("c");
    
    [BackgroundProcess(300)]
    public void SendUpdates()
    {
        foreach (var nodeId in NodeIds)
        {
            if (nodeId != NodeId && _pendingUpdates.TryGetValue(nodeId, out var update) && !update.IsEmpty)
            {
                WriteMessage(new
                {
                    Src = NodeId,
                    Dest = nodeId,
                    Body = new { Type = "update", Update = update, MsgId = NextMsgId() }
                });
            }
        }
    }
    
    [MessageHandler("update")]
    public void HandleUpdate(dynamic msg)
    {
        ImmutableHashSet<long> update = msg.Body.Update.ToObject<ImmutableHashSet<long>>();
        ImmutableInterlocked.Update(ref _messages, messages => messages.Union(update));
        WriteResponse(msg, new { Type = "update_ok", Update = update, InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("update_ok")]
    public void HandleUpdateOk(dynamic msg)
    {
        ImmutableHashSet<long> update = msg.Body.Update.ToObject<ImmutableHashSet<long>>();
        var src = (string)msg.Src;
        ImmutableInterlocked.Update(ref _pendingUpdates, pendingUpdates 
            => pendingUpdates.SetItem(src, pendingUpdates[src].Except(update)));
    }

    [MessageHandler("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        var message = (long)msg.Body.Message;
        ImmutableInterlocked.Update(ref _messages, messages => messages.Add(message));
        
        // Broadcast message to leaders
        if (IsFromClient(msg))
        {
            foreach (var nodeId in Leaders())
            {
                if (nodeId != NodeId)
                {
                    SendRequest(new
                    {
                        Src = NodeId,
                        Dest = nodeId,
                        Body = new { Type = "broadcast", Message = message, MsgId = NextMsgId() }
                    });
                }
            }
        }

        if (IsLeader(NodeId))
        {
            foreach (var nodeId in Nodes(Group(NodeId)))
            {
                if (nodeId != NodeId)
                {
                    ImmutableInterlocked.Update(ref _pendingUpdates,
                        pendingUpdates => pendingUpdates.SetItem(nodeId, pendingUpdates[nodeId].Add(message)));
                }
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
    
    [MessageHandler("init")]
    public void HandleInit(dynamic msg)
    {
        NodeId = msg.Body.NodeId;
        NodeIds = msg.Body.NodeIds.ToObject<string[]>();
        _pendingUpdates = NodeIds.ToImmutableDictionary(nodeId => nodeId, _ => ImmutableHashSet<long>.Empty);
        WriteResponse(msg, new { Type = "init_ok", InReplyTo = msg.Body.MsgId });
    }
}