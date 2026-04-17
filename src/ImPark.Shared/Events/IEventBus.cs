namespace ImPark.Shared.Events;

public interface IEventBus
{
    void PublishSync<T>(in T evt) where T : struct;
    void PublishAsync<T>(in T evt) where T : struct;
    IDisposable Subscribe<T>(Action<T> handler) where T : struct;
    void Flush();
}
