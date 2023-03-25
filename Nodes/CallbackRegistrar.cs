namespace Nodes;

public class CallbackRegistrar
{
    private readonly Node _node;
    private readonly dynamic _msg;
    
    public CallbackRegistrar(Node node, dynamic msg) => (_node, _msg) = (node, msg);

    public void Retry(long delayMillis, int maxRetries = -1)
    {
        _node.Schedule(new Job
        {
            Callback = numRetries =>
            {
                if (!_node.PendingReplyIds.Contains(_msg.Body.MsgId)) return false;
                _node.Send(_msg);
                return maxRetries == -1 || numRetries + 1 < maxRetries;
            },
            Delay = TimeSpan.FromMilliseconds(delayMillis)
        });
    }
}