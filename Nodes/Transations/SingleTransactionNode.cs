using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
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

    public class Operation
    {
        public string Type { get; }
        public int First { get; }
        public int? Second { get; set; }

        public Operation(string[] rawOperation)
        {
            Type = rawOperation[0];
            First = int.Parse(rawOperation[1]);
            Second = rawOperation[2] == null ? null : int.Parse(rawOperation[2]);
        }
        
        public Array ToArray() => new dynamic[] { Type, First, Second.HasValue ? Second : "null" };
        
        public override string ToString() 
            => $"""["{Type}", {First}, {(Second.HasValue ? Second.ToString() : "null")}]""";
    }
}