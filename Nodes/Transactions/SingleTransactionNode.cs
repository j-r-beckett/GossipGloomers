using System.Collections.Concurrent;
using static Nodes.IO;

namespace Nodes.Transations;

public class SingleTransactionNode : InitNode
{
    private ConcurrentDictionary<int, int> _kvStore = new();
    
    [MessageHandler("txn")]
    public void HandleTxm(dynamic msg)
    {
        string[][] rawOperations = msg.Body.Txn.ToObject<string[][]>();
        var operations = rawOperations.Select(op => new Operation(op));  // mutated
        foreach (var op in operations)
        {
            switch (op.Type)
            {
                case "r":
                    if (_kvStore.TryGetValue(op.First, out var result))
                    {
                        op.Second = result;
                    }
                    break;
                case "w":
                    _kvStore[op.First] = op.Second.Value;
                    break;
            }
        }

        WriteResponse(msg, new
        {
            Type = "txn_ok", InReplyTo = msg.Body.MsgId, Txn = operations.Select(op => op.ToArray()).ToArray()
        });
    }
}