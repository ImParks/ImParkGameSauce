using FluentAssertions;
using ImPark.Pawn.Application.AI;
using ImPark.Pawn.Domain.Events;
using ImPark.Shared.Events;
using Xunit;

namespace ImPark.Pawn.Tests.AI;

public sealed class ReservationServiceTests
{
    private static (ReservationService svc, IEventBus bus) Build()
    {
        var bus = new EventBus();
        var svc = new ReservationService(bus);
        return (svc, bus);
    }

    [Fact]
    public void TryReserve_Succeeds_Once()
    {
        var (svc, _) = Build();
        svc.TryReserve(1, 100, "job-a").Should().BeTrue();
    }

    [Fact]
    public void TryReserve_Fails_When_Target_Already_Reserved()
    {
        var (svc, _) = Build();
        svc.TryReserve(1, 100, "job-a").Should().BeTrue();
        svc.TryReserve(2, 100, "job-b").Should().BeFalse();
    }

    [Fact]
    public void ReleaseAllByPawn_Clears_All()
    {
        var (svc, _) = Build();
        svc.TryReserve(1, 100, "a");
        svc.TryReserve(1, 101, "a");
        svc.ReleaseAllByPawn(1);
        svc.IsReserved(100).Should().BeFalse();
        svc.IsReserved(101).Should().BeFalse();
    }

    [Fact]
    public void AutoRelease_On_PawnDied()
    {
        var (svc, bus) = Build();
        svc.TryReserve(1, 100, "a");
        bus.PublishSync(new PawnDiedEvent(1, "cause"));
        svc.IsReserved(100).Should().BeFalse();
    }

    [Fact]
    public void AutoRelease_On_PawnDowned()
    {
        var (svc, bus) = Build();
        svc.TryReserve(1, 100, "a");
        bus.PublishSync(new PawnDownedEvent(1));
        svc.IsReserved(100).Should().BeFalse();
    }

    [Fact]
    public void AutoRelease_On_PawnDraftChanged()
    {
        var (svc, bus) = Build();
        svc.TryReserve(1, 100, "a");
        bus.PublishSync(new PawnDraftChangedEvent(1, true));
        svc.IsReserved(100).Should().BeFalse();
    }

    [Fact]
    public void IsReserved_Reflects_State()
    {
        var (svc, _) = Build();
        svc.IsReserved(100).Should().BeFalse();
        svc.TryReserve(1, 100, "a");
        svc.IsReserved(100).Should().BeTrue();
        svc.Release(1, 100);
        svc.IsReserved(100).Should().BeFalse();
    }

    [Fact]
    public void ReleaseAllByJob_Clears_Only_Matching_Job()
    {
        var (svc, _) = Build();
        svc.TryReserve(1, 100, "a");
        svc.TryReserve(1, 101, "b");
        svc.ReleaseAllByJob("a");
        svc.IsReserved(100).Should().BeFalse();
        svc.IsReserved(101).Should().BeTrue();
    }

    [Fact]
    public void Release_Wrong_Pawn_Does_Not_Release()
    {
        var (svc, _) = Build();
        svc.TryReserve(1, 100, "a");
        svc.Release(2, 100);
        svc.IsReserved(100).Should().BeTrue();
    }
}
