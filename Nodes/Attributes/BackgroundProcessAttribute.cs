namespace Nodes;

[AttributeUsage(AttributeTargets.Method)]
public class BackgroundProcessAttribute : Attribute
{
    public BackgroundProcessAttribute(long intervalMillis)
    {
        IntervalMillis = intervalMillis;
    }

    public long IntervalMillis { get; }
}