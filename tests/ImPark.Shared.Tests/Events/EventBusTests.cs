using FluentAssertions;
using ImPark.Shared.Events;
using Xunit;

namespace ImPark.Shared.Tests.Events;

[Event("test.sample")]
public readonly record struct SampleEvent(int Value);

[Event("test.other")]
public readonly record struct OtherEvent(string Message);

public readonly record struct UntaggedEvent(int Value);

public sealed class EventBusTests
{
    private readonly EventBus bus = new();

    [Fact]
    public void PublishSync_InvokesHandler_BeforeReturning()
    {
        var received = false;
        bus.Subscribe<SampleEvent>(e =>
        {
            received = true;
        });

        bus.PublishSync(new SampleEvent(42));

        received.Should().BeTrue("handler must be invoked synchronously before PublishSync returns");
    }

    [Fact]
    public void PublishSync_PassesCorrectPayload()
    {
        int capturedValue = 0;
        bus.Subscribe<SampleEvent>(e => capturedValue = e.Value);

        bus.PublishSync(new SampleEvent(99));

        capturedValue.Should().Be(99);
    }

    [Fact]
    public void PublishAsync_DoesNotDispatch_UntilFlush()
    {
        var received = false;
        bus.Subscribe<SampleEvent>(e => received = true);

        bus.PublishAsync(new SampleEvent(1));

        received.Should().BeFalse("async events must not dispatch until Flush is called");

        bus.Flush();

        received.Should().BeTrue("async events must dispatch after Flush");
    }

    [Fact]
    public void Flush_DrainsInFifoOrder()
    {
        var order = new List<int>();
        bus.Subscribe<SampleEvent>(e => order.Add(e.Value));

        bus.PublishAsync(new SampleEvent(1));
        bus.PublishAsync(new SampleEvent(2));
        bus.PublishAsync(new SampleEvent(3));

        bus.Flush();

        order.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Dispose_Unsubscribes_Handler()
    {
        int callCount = 0;
        var sub = bus.Subscribe<SampleEvent>(e => callCount++);

        bus.PublishSync(new SampleEvent(1));
        callCount.Should().Be(1);

        sub.Dispose();

        bus.PublishSync(new SampleEvent(2));
        callCount.Should().Be(1, "disposed subscription must not receive further events");
    }

    [Fact]
    public void MultipleSubscribers_AllCalled()
    {
        int countA = 0;
        int countB = 0;
        int countC = 0;

        bus.Subscribe<SampleEvent>(_ => countA++);
        bus.Subscribe<SampleEvent>(_ => countB++);
        bus.Subscribe<SampleEvent>(_ => countC++);

        bus.PublishSync(new SampleEvent(1));

        countA.Should().Be(1);
        countB.Should().Be(1);
        countC.Should().Be(1);
    }

    [Fact]
    public void HandlerException_DoesNotBreak_OtherHandlers()
    {
        int firstCalled = 0;
        int thirdCalled = 0;

        bus.Subscribe<SampleEvent>(_ => firstCalled++);
        bus.Subscribe<SampleEvent>(_ => throw new InvalidOperationException("boom"));
        bus.Subscribe<SampleEvent>(_ => thirdCalled++);

        bus.PublishSync(new SampleEvent(1));

        firstCalled.Should().Be(1);
        thirdCalled.Should().Be(1, "exception in one handler must not prevent subsequent handlers");
    }

    [Fact]
    public void Subscribe_DifferentEventTypes_AreIsolated()
    {
        int sampleCount = 0;
        string? otherMessage = null;

        bus.Subscribe<SampleEvent>(_ => sampleCount++);
        bus.Subscribe<OtherEvent>(e => otherMessage = e.Message);

        bus.PublishSync(new SampleEvent(1));
        bus.PublishSync(new OtherEvent("hello"));

        sampleCount.Should().Be(1);
        otherMessage.Should().Be("hello");
    }

    [Fact]
    public void Flush_WithNoQueuedEvents_DoesNothing()
    {
        int callCount = 0;
        bus.Subscribe<SampleEvent>(_ => callCount++);

        bus.Flush();

        callCount.Should().Be(0);
    }

    [Fact]
    public void UntaggedEvent_StillWorks_ButLogsWarning()
    {
        int received = 0;
        bus.Subscribe<UntaggedEvent>(e => received = e.Value);

        bus.PublishSync(new UntaggedEvent(77));

        received.Should().Be(77, "untagged events should still dispatch, just with a warning");
    }

    [Fact]
    public void PublishSync_WithNoSubscribers_DoesNotThrow()
    {
        var act = () => bus.PublishSync(new SampleEvent(1));
        act.Should().NotThrow();
    }

    [Fact]
    public void AsyncQueue_MultipleFlushes_WorkCorrectly()
    {
        var values = new List<int>();
        bus.Subscribe<SampleEvent>(e => values.Add(e.Value));

        bus.PublishAsync(new SampleEvent(10));
        bus.Flush();

        bus.PublishAsync(new SampleEvent(20));
        bus.Flush();

        values.Should().Equal(10, 20);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var sub = bus.Subscribe<SampleEvent>(_ => { });
        sub.Dispose();

        var act = () => sub.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void HandlerException_DoesNotBreak_AsyncFlush()
    {
        var results = new List<int>();

        bus.Subscribe<SampleEvent>(e =>
        {
            if (e.Value == 2) throw new Exception("async boom");
            results.Add(e.Value);
        });

        bus.PublishAsync(new SampleEvent(1));
        bus.PublishAsync(new SampleEvent(2));
        bus.PublishAsync(new SampleEvent(3));

        bus.Flush();

        results.Should().Equal(1, 3);
    }
}
