using Newtonsoft.Json;

namespace Nodes;

public static class MaelstromUtils
{
    public static void Send(dynamic msg)
    {
        var msgJson = JsonConvert.SerializeObject(msg);
        // Log($"sending msg {msgJson}");
        Console.WriteLine(msgJson);
    }

    public static void Log(string s)
    {
        Console.Error.WriteLine(s);
    }
}