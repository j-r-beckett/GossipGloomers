namespace Nodes;

public class CallbackRegistrar
{
    private readonly Node _node;
    private readonly dynamic _msg;
    
    public CallbackRegistrar(Node node, dynamic msg) => (_node, _msg) = (node, msg);

    public void EnableRetry(long delayMillis, int maxRetries = -1)
    {
        _node.PendingReplyIds.Add(_msg.Body.MsgId);
        Action<dynamic> responseHandler = msg =>
        {
            _node.PendingReplyIds.Remove((long)msg.Body.InReplyTo);
        };
        _node.AddResponseHandler(_msg.Body.Type + "_ok", _msg.Body.MsgId, responseHandler);
        
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

    public void OnReponse(string type, long inReplyTo, Action<dynamic> handler)
    {
        _node.AddResponseHandler(type, inReplyTo, handler);
    }
}