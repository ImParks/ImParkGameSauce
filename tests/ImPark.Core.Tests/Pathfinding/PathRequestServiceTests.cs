using FluentAssertions;
using ImPark.Core.Pathfinding.Application;
using ImPark.Core.Pathfinding.Domain;
using ImPark.Shared.Geometry;
using ImPark.Shared.Random;
using Xunit;

namespace ImPark.Core.Tests.Pathfinding;

public class PathRequestServiceTests
{
    private static (PathRequestService svc, StubEventBus bus, StubTilemap map) BuildService(
        int w = 20, int h = 20, bool defaultWalkable = true)
    {
        var map = new StubTilemap(w, h, defaultWalkable);
        var rng = new XorShift32(7);
        var astar = new AStarPathfinder(map, rng);
        var reach = new RegionReachabilityChecker(map);
        var cache = new PathCache();
        var bus = new StubEventBus();
        var svc = new PathRequestService(astar, reach, cache, bus, map);
        return (svc, bus, map);
    }

    [Fact]
    public void Budget_EnforcesMaxEightPerTick()
    {
        var (svc, _, _) = BuildService();

        int completed = 0;
        for (int i = 0; i < 100; i++)
            svc.Request(i, new Point(0, 0), new Point(5, 0), priority: 0,
                cb: _ => Interlocked.Increment(ref completed));

        svc.Tick(PathRequestService.DefaultBudgetMs);

        completed.Should().BeLessThanOrEqualTo(PathRequestService.MaxRequestsPerTick);
        completed.Should().Be(PathRequestService.MaxRequestsPerTick);
    }

    [Fact]
    public void RegionPruning_UnreachableGoalFailsFast()
    {
        var (svc, _, map) = BuildService();
        // Isolate (10,10) in region 2 by walling around it, except itself.
        for (int y = 0; y < 20; y++) map.SetWall(9, y);
        map.SetRegion(10, 10, 2);

        PathResult? observed = null;
        svc.Request(1, new Point(0, 0), new Point(10, 10), priority: 0,
            cb: r => observed = r);

        svc.Tick(PathRequestService.DefaultBudgetMs);

        observed.Should().NotBeNull();
        observed!.Value.Success.Should().BeFalse();
        observed.Value.Reason.Should().Be(PathFailReason.RegionUnreachable);
    }

    [Fact]
    public void Cancel_CancelledRequestCallbackNotInvoked()
    {
        var (svc, _, _) = BuildService();

        bool called = false;
        var h = svc.Request(1, new Point(0, 0), new Point(5, 5), priority: 0,
            cb: _ => called = true);

        svc.Cancel(h);
        svc.Tick(PathRequestService.DefaultBudgetMs);

        called.Should().BeFalse();
    }

    [Fact]
    public void RequestToNearestOf_ReturnsClosestGoal()
    {
        var (svc, _, _) = BuildService();

        PathResult? observed = null;
        var goals = new[] { new Point(10, 10), new Point(2, 0), new Point(15, 15) };
        svc.RequestToNearestOf(1, new Point(0, 0), goals, priority: 0,
            cb: r => observed = r);

        svc.Tick(PathRequestService.DefaultBudgetMs);

        observed.Should().NotBeNull();
        observed!.Value.Success.Should().BeTrue();
        // Nearest goal is (2,0): cost 2, 3 waypoints.
        observed.Value.Waypoints[^1].Should().Be(new Point(2, 0));
        observed.Value.Cost.Should().BeApproximately(2.0f, 0.0001f);
    }

    [Fact]
    public void IsReachable_UnreachableReturnsFalse()
    {
        var (svc, _, map) = BuildService();
        for (int y = 0; y < 20; y++) map.SetWall(9, y);
        map.SetRegion(10, 10, 2);

        svc.IsReachable(new Point(0, 0), new Point(10, 10)).Should().BeFalse();
        svc.IsReachable(new Point(0, 0), new Point(5, 5)).Should().BeTrue();
    }
}
