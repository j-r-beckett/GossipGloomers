using System.Collections.Concurrent;
using static Nodes.IO;

namespace Nodes.Kafka;

// Partitions logs across nodes. Works great, but not the intended way to solve this challenge
public class DishonestMultiKafkaNode : InitNode
{
    private readonly ConcurrentDictionary<string, long> _commits = new();
    private readonly ConcurrentDictionary<string, List<long>> _logs = new();
    
    private string Host(string log) => $"n{Math.Abs(Hash(log) % NodeIds.Length)}";

    // https://stackoverflow.com/a/36846609
    private static long Hash(string str)
    {
        unchecked
        {
            int hash1 = 5381;
            int hash2 = hash1;

            for(int i = 0; i < str.Length && str[i] != '\0'; i += 2)
            {
                hash1 = ((hash1 << 5) + hash1) ^ str[i];
                if (i == str.Length - 1 || str[i+1] == '\0')
                    break;
                hash2 = ((hash2 << 5) + hash2) ^ str[i+1];
            }

            return hash1 + (hash2*1566083941);
        }
    }
    
    private static bool IsMessageFromClient(dynamic msg) => msg.Src.ToString().StartsWith("c");

    [MessageHandler("send")]
    public void HandleSend(dynamic msg)
    {
        var log = (string)msg.Body.Key;
        var entry = (long)msg.Body.Msg;
        long offset;
        
        if (Host(log) == NodeId)
        {
            _logs.TryAdd(log, new List<long>());
            var logEntries = _logs[log];
            logEntries.Add(entry);
            offset = logEntries.Count - 1;
        }
        else
        {
            var response = SendRequest(new
                {
                    Src = NodeId,
                    Dest = Host(log),
                    Body = new { Type = "send", Key = log, Msg = entry, MsgId = NextMsgId() }
                })
                .Wait();
            offset = response.Body.Offset;
        }
        
        WriteResponse(msg, new { Type = "send_ok", Offset = offset, InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("poll")]
    public void HandlePoll(dynamic msg)
    {
        Dictionary<string, long> offsets = msg.Body.Offsets.ToObject<Dictionary<string, long>>();
        var results = new Dictionary<string, List<long[]>>();

        // Add local logs to results
        foreach (var log in offsets.Keys.Where(log => Host(log) == NodeId))
        {
            if (_logs.TryGetValue(log.ToLower(), out var logEntries))
            {
                var messages = new List<long[]>();
                for (var i = (int)offsets[log]; i < logEntries.Count; i++)
                {
                    messages.Add(new[] { i, logEntries[i] });
                }
                results.Add(log.ToLower(), messages);
            }
        }
        
        // Send requests to other nodes to get logs NOT stored on this node
        var responseFutures = offsets.Keys.Select(Host)
            .Distinct()
            .Where(host => host != NodeId)
            .Select(host => SendRequest(new 
            {
                Src = NodeId,
                Dest = host,
                Body = new
                {
                    Type = "poll",
                    Offsets = new Dictionary<string, long>(offsets.Where(e => Host(e.Key) == host)),
                    MsgId = NextMsgId()
                }
            }))
            .ToArray();
        
        // Add logs from other nodes to results
        foreach (var response in MessageProcessor.ResponseFuture.WaitAll(responseFutures))
        {
            Dictionary<string, List<long[]>> msgs = response.Body.Msgs.ToObject<Dictionary<string, List<long[]>>>();
            foreach (var log in msgs.Keys)
            {
                results.Add(log, msgs[log]);
            }
        }
        
        WriteResponse(msg, new { Type = "poll_ok", Msgs = results, InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("commit_offsets")]
    public void HandleCommit(dynamic msg)
    {
        Dictionary<string, int> offsets = msg.Body.Offsets.ToObject<Dictionary<string, int>>();
        foreach (var (key, offset) in offsets)
        {
            _commits[key.ToLower()] = offset;
        }
        
        if (IsMessageFromClient(msg))
        {
            WriteResponse(msg, new { Type = "commit_offsets_ok", InReplyTo = msg.Body.MsgId });
        }
        else
        {
            foreach (var nodeId in NodeIds)
            {
                if (nodeId != NodeId)
                {
                    WriteMessage(new
                    {
                        Src = NodeId,
                        Dest = nodeId,
                        Body = new { Type = "commit_offsets", Offsets = offsets, MsgId = NextMsgId() }
                    });
                }
            }
        }
    }

    [MessageHandler("list_committed_offsets")]
    public void HandleList(dynamic msg)
    {
        string[] keys = msg.Body.Keys.ToObject<string[]>();
        var offsets = keys.Where(k => _commits.ContainsKey(k)).ToDictionary(k => k, k => _commits[k]);
        WriteResponse(msg, new { Type = "list_committed_offsets_ok", Offsets = offsets, InReplyTo = msg.Body.MsgId });
    }
}