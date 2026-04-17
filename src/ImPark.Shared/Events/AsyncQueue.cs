namespace ImPark.Shared.Events;

internal sealed class AsyncQueue
{
    private const int DefaultCapacity = 4096;

    private readonly QueueEntry[] buffer;
    private readonly int capacity;
    private int head;
    private int tail;
    private int count;

    public AsyncQueue(int capacity = DefaultCapacity)
    {
        this.capacity = capacity;
        buffer = new QueueEntry[capacity];
    }

    public int Count => count;

    public void Enqueue<T>(in T evt, Action<object> dispatchCallback) where T : struct
    {
        if (count >= capacity)
            throw new InvalidOperationException(
                $"AsyncQueue is full ({capacity} items). This indicates events are not being flushed.");

        buffer[tail] = new QueueEntry(evt, dispatchCallback);
        tail = (tail + 1) % capacity;
        count++;
    }

    public void DrainAll()
    {
        while (count > 0)
        {
            var entry = buffer[head];
            buffer[head] = default;
            head = (head + 1) % capacity;
            count--;

            entry.Dispatch();
        }
    }
}

internal readonly struct QueueEntry
{
    private readonly object payload;
    private readonly Action<object> dispatchCallback;

    public QueueEntry(object payload, Action<object> dispatchCallback)
    {
        this.payload = payload;
        this.dispatchCallback = dispatchCallback;
    }

    public void Dispatch()
    {
        dispatchCallback(payload);
    }
}
