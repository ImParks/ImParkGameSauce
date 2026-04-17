using FluentAssertions;
using ImPark.Core.Time;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using ImPark.Shared.Queries;

namespace ImPark.Core.Tests.Time;

public class TickSystemTests
{
    private sealed class RecordingEventBus : IEventBus
    {
        public List<TimeTickEvent> Ticks { get; } = new();

        public void PublishSync<T>(in T evt) where T : struct
        {
            if (evt is TimeTickEvent tick)
                Ticks.Add(tick);
        }

        public void PublishAsync<T>(in T evt) where T : struct => PublishSync(in evt);
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct => new DummyDisposable();
        public void Flush() { }

        private sealed class DummyDisposable : IDisposable { public void Dispose() { } }
    }

    private static (World world, RecordingEventBus bus, TickSystem sys) Setup()
    {
        var world = new World();
        var bus = new RecordingEventBus();
        var sys = new TickSystem(world, bus);
        return (world, bus, sys);
    }

    [Fact]
    public void Paused_state_emits_no_ticks()
    {
        var (_, bus, sys) = Setup();
        sys.Pause();

        sys.Advance(1.0f);

        bus.Ticks.Should().BeEmpty();
    }

    [Fact]
    public void Paused_speed_emits_no_ticks()
    {
        var (_, bus, sys) = Setup();
        sys.SetSpeed(GameSpeed.Paused);

        sys.Advance(1.0f);

        bus.Ticks.Should().BeEmpty();
    }

    [Fact]
    public void Normal_speed_emits_one_tick_per_200ms()
    {
        var (_, bus, sys) = Setup();
        sys.SetSpeed(GameSpeed.Normal);

        sys.Advance(0.2f);

        var distinctTicks = bus.Ticks.Select(t => t.Tick).Distinct().ToList();
        distinctTicks.Should().HaveCount(1);
        distinctTicks[0].Should().Be(1);
    }

    [Fact]
    public void Fast_speed_emits_two_ticks_per_200ms()
    {
        var (_, bus, sys) = Setup();
        sys.SetSpeed(GameSpeed.Fast);

        sys.Advance(0.2f);

        var distinctTicks = bus.Ticks.Select(t => t.Tick).Distinct().ToList();
        distinctTicks.Should().HaveCount(2);
        distinctTicks.Should().ContainInOrder(1L, 2L);
    }

    [Fact]
    public void SuperFast_speed_emits_three_ticks_per_200ms()
    {
        var (_, bus, sys) = Setup();
        sys.SetSpeed(GameSpeed.SuperFast);

        // Slightly padded to avoid float-precision boundary (0.2/3 * 3 drifts above 0.2f).
        sys.Advance(0.21f);

        var distinctTicks = bus.Ticks.Select(t => t.Tick).Distinct().ToList();
        distinctTicks.Should().HaveCount(3);
        distinctTicks.Should().ContainInOrder(1L, 2L, 3L);
    }

    [Fact]
    public void Tick_is_monotonically_increasing()
    {
        var (_, bus, sys) = Setup();
        sys.SetSpeed(GameSpeed.Normal);

        for (int i = 0; i < 10; i++)
            sys.Advance(0.2f);

        var distinctTicks = bus.Ticks.Select(t => t.Tick).Distinct().ToList();
        for (int i = 1; i < distinctTicks.Count; i++)
            distinctTicks[i].Should().BeGreaterThan(distinctTicks[i - 1]);
    }

    [Fact]
    public void Each_tick_emits_all_five_phases_in_order()
    {
        var (_, bus, sys) = Setup();
        sys.SetSpeed(GameSpeed.Normal);

        sys.Advance(0.2f);

        bus.Ticks.Should().HaveCount(5);
        bus.Ticks[0].Phase.Should().Be(TickPhase.Input);
        bus.Ticks[1].Phase.Should().Be(TickPhase.AI);
        bus.Ticks[2].Phase.Should().Be(TickPhase.Movement);
        bus.Ticks[3].Phase.Should().Be(TickPhase.Events);
        bus.Ticks[4].Phase.Should().Be(TickPhase.Render);
        bus.Ticks.Select(t => t.Tick).Should().AllBeEquivalentTo(1L);
    }

    [Fact]
    public void Two_ticks_emit_ten_events_with_phases_in_order_per_tick()
    {
        var (_, bus, sys) = Setup();
        sys.SetSpeed(GameSpeed.Fast);

        sys.Advance(0.2f);

        bus.Ticks.Should().HaveCount(10);

        var tick1 = bus.Ticks.Take(5).ToList();
        var tick2 = bus.Ticks.Skip(5).Take(5).ToList();

        tick1.Select(t => t.Phase).Should().ContainInOrder(
            TickPhase.Input, TickPhase.AI, TickPhase.Movement, TickPhase.Events, TickPhase.Render);
        tick2.Select(t => t.Phase).Should().ContainInOrder(
            TickPhase.Input, TickPhase.AI, TickPhase.Movement, TickPhase.Events, TickPhase.Render);

        tick1.Select(t => t.Tick).Should().AllBeEquivalentTo(1L);
        tick2.Select(t => t.Tick).Should().AllBeEquivalentTo(2L);
    }

    [Fact]
    public void SetSpeed_changes_tick_rate()
    {
        var (_, bus, sys) = Setup();

        sys.SetSpeed(GameSpeed.Normal);
        sys.Advance(0.2f);
        var ticksAfterNormal = bus.Ticks.Select(t => t.Tick).Distinct().Count();

        sys.SetSpeed(GameSpeed.Fast);
        sys.Advance(0.2f);
        var totalDistinct = bus.Ticks.Select(t => t.Tick).Distinct().Count();

        ticksAfterNormal.Should().Be(1);
        // After switching to Fast and advancing another 200ms, we should have gained 2 more ticks.
        totalDistinct.Should().Be(3);
    }

    [Fact]
    public void Resume_from_pause_continues_from_previous_tick()
    {
        var (_, bus, sys) = Setup();
        sys.SetSpeed(GameSpeed.Normal);

        sys.Advance(0.2f);
        var lastTickBefore = bus.Ticks.Max(t => t.Tick);

        sys.Pause();
        sys.Advance(1.0f);
        bus.Ticks.Count(t => t.Tick > lastTickBefore).Should().Be(0);

        sys.Resume();
        sys.Advance(0.2f);

        var lastTickAfter = bus.Ticks.Max(t => t.Tick);
        lastTickAfter.Should().Be(lastTickBefore + 1);
    }

    [Fact]
    public void TimeQuery_reads_state_correctly()
    {
        var (world, _, sys) = Setup();
        var query = new TimeQuery(world, sys.TimeEntity);

        query.GetCurrentTick().Should().Be(0);
        query.GetSpeed().Should().Be(GameSpeed.Normal);
        query.IsPaused().Should().BeFalse();

        sys.SetSpeed(GameSpeed.Fast);
        sys.Advance(0.2f);

        query.GetCurrentTick().Should().Be(2);
        query.GetSpeed().Should().Be(GameSpeed.Fast);
        query.IsPaused().Should().BeFalse();

        sys.Pause();
        query.IsPaused().Should().BeTrue();
    }

    [Fact]
    public void Order_is_int_min_value()
    {
        var (_, _, sys) = Setup();
        sys.Order.Should().Be(int.MinValue);
    }
}
