using System.Collections.Concurrent;
using System.Diagnostics;
using Newtonsoft.Json;
using static Nodes.IO;

namespace Nodes.Kafka;

public class MultiKafkaNode : InitNode
{
    private readonly ConcurrentDictionary<string, long> _commits = new();
    private readonly ConcurrentDictionary<string, List<long>> _logs = new();

    private static string LockName => "lock";
    private static string LogOffsetCounterName(string log) => $"{log}-offset-counter";
    private static string LogEntryName(string log, long offset) => $"{log}-{offset}";
    private static bool IsMessageFromClient(dynamic msg) => msg.Src.ToString().StartsWith("c");
    private readonly ConcurrentDictionary<string, long> _pollCache = new ();


    [MessageHandler("send")]
    public void HandleSend(dynamic msg)
    {
        var log = (string)msg.Body.Key;
        
        AcquireLock(LockName);
        
        // read offset counter
        var offsetCounterResponse = SendRequest(new
            {
                Src = NodeId,
                Dest = "lin-kv",
                Body = new { Type = "read", Key = LogOffsetCounterName(log), MsgId = NextMsgId() }
            })
            .Wait();
        var offset = offsetCounterResponse.Body.Type == "error" ? 0 : (int)offsetCounterResponse.Body.Value;
        
        // increment offset counter
        SendRequest(new
            {
                Src = NodeId,
                Dest = "lin-kv",
                Body = new { Type = "write", Key = LogOffsetCounterName(log), Value = offset + 1, MsgId = NextMsgId() }
            })
            .Wait();

        // write message to storage
        SendRequest(new
            {
                Src = NodeId,
                Dest = "lin-kv",
                Body = new
                {
                    Type = "write", Key = LogEntryName(log, offset), Value = msg.Body.Msg, MsgId = NextMsgId()
                }
            })
            .Wait();
        
        ReleaseLock(LockName);
        
        WriteResponse(msg, new { Type = "send_ok", Offset = offset, InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("poll")]
    public void HandlePoll(dynamic msg)
    {
        Dictionary<string, long> offsets = msg.Body.Offsets.ToObject<Dictionary<string, long>>();  // { log -> offset }
        var messages = offsets.Keys.ToDictionary(log => log, _ => new List<long[]>());  // { log -> [(offset, msg)] }

        foreach (var (log, startingOffset) in offsets)
        {
            for (var currentOffset = startingOffset;; currentOffset++)
            {
                var key = LogEntryName(log, currentOffset);
                if (!_pollCache.ContainsKey(key))
                {
                    var readMsgResponse = SendRequest(new
                        {
                            Src = NodeId,
                            Dest = "lin-kv",
                            Body = new { Type = "read", Key = key, MsgId = NextMsgId() }
                        })
                        .Wait();
                    if (readMsgResponse.Body.Type == "error") break;  // stop when we reach the end of the log
                    _pollCache[key] = readMsgResponse.Body.Value;
                }
                messages[log].Add(new long[] { currentOffset, _pollCache[key] });
            } 
        }
        
        // Console.Error.WriteLine($"returning poll {JsonConvert.SerializeObject(messages)} for request {JsonConvert.SerializeObject(msg)}");
        WriteResponse(msg, new { Type = "poll_ok", Msgs = messages, InReplyTo = msg.Body.MsgId });
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
    
    private void AcquireLock(string lockName)
    {
        dynamic response;
        do
        {
            response = SendRequest(new
                {
                    Src = NodeId,
                    Dest = "lin-kv",
                    Body = new
                    {
                        Type = "cas",
                        Key = lockName,
                        From = "unlocked",
                        To = "locked",
                        Create_If_Not_Exists = true,
                        MsgId = NextMsgId()
                    }
                })
                .Wait();
        } while (response.Body.Type == "error");  // attempt to acquire lock until successful
    }

    private void ReleaseLock(string lockName)
    {
        SendRequest(new
            {
                Src = NodeId,
                Dest = "lin-kv",
                Body = new { Type = "write", Key = lockName, Value = "unlocked", MsgId = NextMsgId() }
            })
            .Wait();
    }
}