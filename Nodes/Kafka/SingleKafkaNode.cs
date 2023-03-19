namespace Nodes.Kafka;

public class SingleKafkaNode : Node
{
    private readonly Dictionary<string, int> _commits = new();
    private readonly Dictionary<string, List<int>> _logs = new();

    [MessageHandler("send")]
    public void HandleSend(dynamic msg)
    {
        var key = (string)msg.Body.Key;
        if (!_logs.TryGetValue(key, out var log))
        {
            log = new List<int>();
            _logs.Add(key, log);
        }

        log.Add((int)msg.Body.Msg);
        Reply(new { Type = "send_ok", Offset = log.Count - 1, InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("poll")]
    public void HandlePoll(dynamic msg)
    {
        Dictionary<string, int> offsets = msg.Body.Offsets.ToObject<Dictionary<string, int>>();

        var results = new Dictionary<string, List<int[]>>();

        foreach (var (key, offset) in offsets)
            if (_logs.TryGetValue(key.ToLower(), out var log))
            {
                var messages = new List<int[]>();
                for (var i = offset; i < log.Count; i++) messages.Add(new[] { i, log[i] });

                results.Add(key.ToLower(), messages);
            }

        Reply(new { Type = "poll_ok", Msgs = results, InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("commit_offsets")]
    public void HandleCommit(dynamic msg)
    {
        Dictionary<string, int> offsets = msg.Body.Offsets.ToObject<Dictionary<string, int>>();

        foreach (var (key, offset) in offsets) _commits[key.ToLower()] = offset;

        Reply(new { Type = "commit_offsets_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("list_committed_offsets")]
    public void HandleList(dynamic msg)
    {
        string[] keys = msg.Body.Keys.ToObject<string[]>();

        var offsets = keys.Where(k => _commits.ContainsKey(k)).ToDictionary(k => k, k => _commits[k]);

        Reply(new { Type = "list_committed_offsets_ok", Offsets = offsets, InReplyTo = msg.Body.MsgId });
    }
}