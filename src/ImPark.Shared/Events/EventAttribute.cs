namespace ImPark.Shared.Events;

[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
public sealed class EventAttribute : Attribute
{
    public string Topic { get; }

    public EventAttribute(string topic)
    {
        Topic = topic;
    }
}
