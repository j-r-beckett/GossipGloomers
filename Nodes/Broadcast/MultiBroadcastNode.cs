using System.Diagnostics;

namespace Nodes.Broadcast;

public class MultiBroadcastNode : Node
{
    private readonly HashSet<long> _messages = new();
    private readonly Dictionary<long, Stopwatch> _broadcastTripTimes = new();
    private List<long> _pings = new();

    private int _messageId;

    private static int Next(ref int messageId)
    {
        return ++messageId;
    }

    private static bool IsClientBroadcast(dynamic broadcastMsg)
    {
        return broadcastMsg.Src.ToString().StartsWith("c");
    }


    [MessageHandler("broadcast")]
    public void HandleBroadcast(dynamic msg)
    {
        _messages.Add((int)msg.Body.Message);

        if (IsClientBroadcast(msg))
        {
            foreach (var adjNode in NodeIds)
            {
                if (adjNode != NodeId)
                {
                    MaelstromUtils.Send(new
                    {
                        Src = NodeId,
                        Dest = adjNode,
                        Body = new { Type = "broadcast", msg.Body.Message, MsgId = Next(ref _messageId) }
                    });
                    _broadcastTripTimes[_messageId] = Stopwatch.StartNew();
                }
            }
        }
        
        Reply(new { Type = "broadcast_ok", InReplyTo = msg.Body.MsgId });
    }

    [MessageHandler("read")]
    public void HandleRead(dynamic msg)
    {
        Reply(new
        {
            Type = "read_ok",
            Messages = _messages.AsEnumerable().OrderBy(n => n).ToList(), // sort to make it easier to read output
            InReplyTo = msg.Body.MsgId
        });
    }

    [MessageHandler("broadcast_ok")]
    public void HandleBroadcastOk(dynamic msg)
    {
        if (NodeId == "n0")
        {
            var stopwatch = _broadcastTripTimes[(long)msg.Body.InReplyTo];
            var pingMillis = stopwatch.ElapsedMilliseconds;
            _pings.Add(pingMillis);
            Console.Error.WriteLine(string.Join(", ", _pings));
        }
    }

    [MessageHandler("topology")]
    public void HandleTopology(dynamic msg)
    {
        Reply(new { Type = "topology_ok", InReplyTo = msg.Body.MsgId });
    }
}