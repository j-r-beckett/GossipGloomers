using Newtonsoft.Json;

namespace Nodes;

public static class IO
{
    public static void WriteResponse(dynamic request, dynamic responseBody) 
        => WriteMessage(new { Src = request.Dest, Dest = request.Src, Body = responseBody });
    
    public static void WriteMessage(dynamic msg) => Console.WriteLine(JsonConvert.SerializeObject(msg));
}