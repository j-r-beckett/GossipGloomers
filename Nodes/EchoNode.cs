using Newtonsoft.Json;

namespace Nodes;

public class EchoNode
{
    private string? _nodeId;

    private void Send(dynamic msg)
    {
        Console.WriteLine(JsonConvert.SerializeObject(msg));
    }

    private void Log(string s)
    {
        Console.Error.WriteLine(s);
    }

    public void ReceiveMessage(dynamic msg)
    {
        Log($"received msg [{msg}]");
        if (msg.body.type == "init")
        {
            _nodeId = msg.body.node_id;
            Log("sending init reply");
            Send(new { src = _nodeId, dest = msg.src, body = new { type = "init_ok", in_reply_to = msg.body.msg_id } });
        }
        else if (msg.body.type == "echo")
        {
            Log("sending echo");
            Send(new
            {
                src = _nodeId, dest = msg.src,
                body = new { type = "echo_ok", in_reply_to = msg.body.msg_id, msg.body.echo }
            });
        }
    }
}