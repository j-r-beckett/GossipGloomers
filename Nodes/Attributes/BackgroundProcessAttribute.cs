namespace Nodes;

[AttributeUsage(System.AttributeTargets.Method)]  
public class BackgroundProcessAttribute : Attribute
{
    public long IntervalMillis { get; }

    public BackgroundProcessAttribute(long intervalMillis) => IntervalMillis = intervalMillis;
}