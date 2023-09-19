using System.Collections.Concurrent;

namespace Nodes;

public class MessageProcessor
{
    private readonly ConcurrentDictionary<string, MutableResponseFuture> _responseFutures = new();
    
    public ResponseFuture ProcessRequest(dynamic request)
        => _responseFutures[UniqueIdFromRequest(request)] = new MutableResponseFuture();

    public bool TryProcessResponse(dynamic response)
    {
        if (_responseFutures.Remove(UniqueIdFromResponse(response), out MutableResponseFuture future))
        {
            future.Response = response;
            return true;
        }
        return false;
    }
    
    public abstract class ResponseFuture
    {
        public abstract bool TryGetResponse(out dynamic? response);

        public dynamic Wait() => WaitAll(this).First();

        public static dynamic[] WaitAll(params ResponseFuture[] futures)
        {
            while (futures.Any(future => !future.TryGetResponse(out _)))
            {
                Thread.Sleep(10);
            }
            
            return futures.Select(future =>
                {
                    future.TryGetResponse(out var response);
                    return response!;
                })
                .ToArray();
        }
    }
    
    private class MutableResponseFuture : ResponseFuture
    {
        public dynamic? Response { get; set; }

        public override bool TryGetResponse(out dynamic? response) => (response = Response) != null;
    }
    
    private static string UniqueIdFromRequest(dynamic request) => $"{request.Dest}-{request.Body.MsgId}";
    private static string UniqueIdFromResponse(dynamic response) => $"{response.Src}-{response.Body.InReplyTo}";
}