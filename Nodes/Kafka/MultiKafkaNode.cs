using System.Collections.Concurrent;
using System.Diagnostics;
using Newtonsoft.Json;
using static Nodes.IO;

namespace Nodes.Kafka;

public class MultiKafkaNode : InitNode
{
    private readonly ConcurrentDictionary<string, long> _commits = new();
    private readonly ConcurrentDictionary<string, List<long>> _logs = new();
    
    private static string LogOffsetCounterName(string log) => $"{log}-offset-counter";
    private static string LogEntryName(string log, long offset) => $"{log}-{offset}";
    private static bool IsMessageFromClient(dynamic msg) => msg.Src.ToString().StartsWith("c");
    private readonly ConcurrentDictionary<string, long> _pollCache = new ();


    [MessageHandler("send")]
    public void HandleSend(dynamic msg)
    {
        var log = (string)msg.Body.Key;

        dynamic incrementResponse;
        int offset;
        do
        {
            // read offset counter
            var offsetCounterResponse = SendRequest(new
                {
                    Src = NodeId,
                    Dest = "lin-kv",
                    Body = new { Type = "read", Key = LogOffsetCounterName(log), MsgId = NextMsgId() }
                })
                .Wait();
            offset = offsetCounterResponse.Body.Type == "error" ? 0 : (int)offsetCounterResponse.Body.Value;
            
            // increment offset counter
            incrementResponse = SendRequest(new
                {
                    Src = NodeId,
                    Dest = "lin-kv",
                    Body = new
                    {
                        Type = "cas",
                        Key = LogOffsetCounterName(log),
                        From = offset,
                        To = offset + 1,
                        Create_If_Not_Exists = true,
                        MsgId = NextMsgId()
                    }
                })
                .Wait();
        } while (incrementResponse.Body.Type == "error");
        
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
        
        WriteResponse(msg, new { Type = "send_ok", Offset = offset, InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("poll")]
    public void HandlePoll(dynamic msg)
    {
        Dictionary<string, long> offsets = msg.Body.Offsets.ToObject<Dictionary<string, long>>();  // { log -> offset }
        // var messageFutures = offsets.Keys.ToDictionary(log => log, _ => new List<MessageProcessor.ResponseFuture[]>());  // { log -> [(offset, ResponseFuture)] }
        var messages = offsets.Keys.ToDictionary(log => log, _ => new List<long[]>());  // { log -> [(offset, msg)] }
        
        foreach (var (log, startingOffset) in offsets)
        {
            var offsetCounterResponse = SendRequest(new
                {
                    Src = NodeId,
                    Dest = "lin-kv",
                    Body = new { Type = "read", Key = LogOffsetCounterName(log), MsgId = NextMsgId() }
                })
                .Wait();
            var endOffset = offsetCounterResponse.Body.Type == "error" ? 0 : (int)offsetCounterResponse.Body.Value;
            for (var currentOffset = startingOffset; currentOffset < endOffset; currentOffset++)
            {
                var key = LogEntryName(log, currentOffset);
                if (!_pollCache.ContainsKey(key))
                {
                    dynamic readMsgResponse;
                    do
                    {
                        readMsgResponse = SendRequest(new
                            {
                                Src = NodeId,
                                Dest = "lin-kv",
                                Body = new
                                {
                                    Type = "read", Key = LogEntryName(log, currentOffset), MsgId = NextMsgId()
                                }
                            })
                            .Wait();
                    } while (readMsgResponse.Body.Type == "error");
                    // _pollCache[key] = readMsgResponse.Body.Value;
                }
                messages[log].Add(new [] { currentOffset, _pollCache[key] });
            } 
        }
        
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
}