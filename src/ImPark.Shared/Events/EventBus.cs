using System.Collections.Concurrent;

namespace ImPark.Shared.Events;

public sealed class EventBus : IEventBus
{
    private readonly SyncDispatcher syncDispatcher = new();
    private readonly AsyncQueue asyncQueue = new();
    private readonly ConcurrentDictionary<Type, bool> validatedTypes = new();

    public void PublishSync<T>(in T evt) where T : struct
    {
        ValidateEventType<T>();
        syncDispatcher.Dispatch(in evt);
    }

    public void PublishAsync<T>(in T evt) where T : struct
    {
        ValidateEventType<T>();

        // Capture the value to box once for the queue
        var boxed = evt;
        asyncQueue.Enqueue(boxed, payload => syncDispatcher.Dispatch((T)payload));
    }

    public IDisposable Subscribe<T>(Action<T> handler) where T : struct
    {
        ValidateEventType<T>();
        return syncDispatcher.Subscribe(handler);
    }

    public void Flush()
    {
        asyncQueue.DrainAll();
    }

    private void ValidateEventType<T>() where T : struct
    {
        validatedTypes.GetOrAdd(typeof(T), type =>
        {
            var attr = Attribute.GetCustomAttribute(type, typeof(EventAttribute)) as EventAttribute;
            if (attr is null)
            {
                Console.Error.WriteLine(
                    $"[EventBus] Warning: {type.Name} is missing [Event(\"topic\")] attribute.");
                return false;
            }
            return true;
        });
    }
}
