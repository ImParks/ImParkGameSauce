using System.Collections.Concurrent;

namespace ImPark.Shared.Events;

internal sealed class SyncDispatcher
{
    private readonly ConcurrentDictionary<Type, object> handlersByType = new();

    public IDisposable Subscribe<T>(Action<T> handler) where T : struct
    {
        var list = (SubscriptionList<T>)handlersByType.GetOrAdd(typeof(T), _ => new SubscriptionList<T>());
        return list.Add(handler);
    }

    public void Dispatch<T>(in T evt) where T : struct
    {
        if (!handlersByType.TryGetValue(typeof(T), out var raw))
            return;

        var list = (SubscriptionList<T>)raw;
        list.Invoke(evt);
    }
}

internal sealed class SubscriptionList<T> where T : struct
{
    private readonly object syncRoot = new();
    private readonly List<SubscriptionEntry<T>> entries = new();

    public IDisposable Add(Action<T> handler)
    {
        var entry = new SubscriptionEntry<T>(handler);
        lock (syncRoot)
        {
            entries.Add(entry);
        }
        return new Unsubscriber<T>(this, entry);
    }

    public void Remove(SubscriptionEntry<T> entry)
    {
        lock (syncRoot)
        {
            entries.Remove(entry);
        }
    }

    public void Invoke(T evt)
    {
        SubscriptionEntry<T>[] snapshot;
        lock (syncRoot)
        {
            snapshot = entries.ToArray();
        }

        for (int i = 0; i < snapshot.Length; i++)
        {
            try
            {
                snapshot[i].Handler(evt);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[EventBus] Handler for {typeof(T).Name} threw: {ex}");
            }
        }
    }
}

internal sealed class SubscriptionEntry<T> where T : struct
{
    public Action<T> Handler { get; }

    public SubscriptionEntry(Action<T> handler)
    {
        Handler = handler;
    }
}

internal sealed class Unsubscriber<T> : IDisposable where T : struct
{
    private SubscriptionList<T>? list;
    private SubscriptionEntry<T>? entry;

    public Unsubscriber(SubscriptionList<T> list, SubscriptionEntry<T> entry)
    {
        this.list = list;
        this.entry = entry;
    }

    public void Dispose()
    {
        list?.Remove(entry!);
        list = null;
        entry = null;
    }
}
