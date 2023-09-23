using System.Collections.Concurrent;
using Newtonsoft.Json;
using static Nodes.IO;

namespace Nodes.Kafka;

public class MultiKafkaNode : Node
{
    private readonly ConcurrentDictionary<string, long> _commits = new();
    private readonly ConcurrentDictionary<string, List<long>> _logs = new();
    
    private string LogCreationLockName => "log-creation-lock";  // lin-kv[LogCreationLockName] = "locked" / "unlocked" 
    private string LogOffsetCounterName(string log) => $"{log}-offset-counter";
    private static string LogEntryName(string log, long offset) => $"{log}-{offset}";
    private static bool IsMessageFromClient(dynamic msg) => msg.Src.ToString().StartsWith("c");


    [MessageHandler("send")]
    public void HandleSend(dynamic msg)
    {
        // Console.Error.WriteLine($"received send {JsonConvert.SerializeObject(msg)}");
        
        var log = (string)msg.Body.Key;
        var getLogOffsetCounterRequest = new
        {
            Src = NodeId,
            Dest = "lin-kv",
            Body = new { Type = "read", Key = LogOffsetCounterName(log), MsgId = NextMsgId() }
        };
        var logOffsetCounter = SendRequest(getLogOffsetCounterRequest).Wait();
        long offset;

        if (logOffsetCounter.Body.Type == "error")
        {
            AcquireLock(LogCreationLockName);
            logOffsetCounter = SendRequest(getLogOffsetCounterRequest).Wait();  // check if another node initialized log
            if (logOffsetCounter.Body.Type == "error")
            {
                WriteMessage(new
                {
                    Src = NodeId,
                    Dest = "lin-kv",
                    Body = new { Type = "write", Key = LogOffsetCounterName(log), Value = 0, MsgId = NextMsgId() }
                });
                logOffsetCounter = SendRequest(getLogOffsetCounterRequest).Wait();
            }
            ReleaseLock(LogCreationLockName);
        }
        
        // increment it
        dynamic IncrementCounterRequest(long offset)
            => new
            {
                Src = NodeId,
                Dest = "lin-kv",
                Body = new
                {
                    Type = "cas",
                    Key = LogOffsetCounterName(log),
                    From = offset,
                    To = offset + 1,
                    MsgId = NextMsgId()
                }
            };

        var incrementCounterResponse = SendRequest(IncrementCounterRequest(logOffsetCounter.Body.Value)).Wait();
        // (re-read log offset and retry increment) until increment succeeds
        while (incrementCounterResponse.Body.Type == "error") 
        {
            logOffsetCounter = SendRequest(getLogOffsetCounterRequest).Wait();
            incrementCounterResponse = SendRequest(IncrementCounterRequest(logOffsetCounter.Body.Value)).Wait();
        }

        offset = logOffsetCounter.Body.Value;
        
        // write message to storage
        WriteMessage(new
        {
            Src = NodeId,
            Dest = "lin-kv",
            Body = new { Type = "write", Key = LogEntryName(log, offset), Value = msg.Body.Msg, MsgId = NextMsgId() }
        });
        
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
                var readMsgResponse = SendRequest(new
                    {
                        Src = NodeId,
                        Dest = "lin-kv",
                        Body = new { Type = "read", Key = LogEntryName(log, currentOffset), MsgId = NextMsgId() }
                    })
                    .Wait();
                if (readMsgResponse.Body.Type == "error") break;  // stop when we reach the end of the log
                messages[log].Add(new long[] { currentOffset, readMsgResponse.Body.Value });
            }; 
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
    
    [MessageHandler("init")]
    public void HandleInit(dynamic msg)
    {
        NodeId = msg.Body.NodeId;
        if (NodeId == "n0")
        {
            WriteMessage(new
            {
                Src = NodeId,
                Dest = "lin-kv",
                Body = new { Type = "write", Key = LogCreationLockName, Value = "unlocked", MsgId = NextMsgId() }
            });
        }
        NodeIds = msg.Body.NodeIds.ToObject<string[]>();
        WriteResponse(msg, new { Type = "init_ok", InReplyTo = msg.Body.MsgId });
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

    [MessageHandler("list_committed_offsets")]
    public void HandleList(dynamic msg)
    {
        string[] keys = msg.Body.Keys.ToObject<string[]>();
        var offsets = keys.Where(k => _commits.ContainsKey(k)).ToDictionary(k => k, k => _commits[k]);
        WriteResponse(msg, new { Type = "list_committed_offsets_ok", Offsets = offsets, InReplyTo = msg.Body.MsgId });
    }
}