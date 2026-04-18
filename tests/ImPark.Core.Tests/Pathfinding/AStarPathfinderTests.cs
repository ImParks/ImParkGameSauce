using FluentAssertions;
using ImPark.Core.Pathfinding.Application;
using ImPark.Core.Pathfinding.Domain;
using ImPark.Shared.Geometry;
using ImPark.Shared.Random;
using Xunit;

namespace ImPark.Core.Tests.Pathfinding;

public class AStarPathfinderTests
{
    private static AStarPathfinder NewPathfinder(StubTilemap map, uint seed = 42)
        => new(map, new XorShift32(seed));

    [Fact]
    public void StraightLine_ReturnsSixWaypointsCostFive()
    {
        var map = new StubTilemap(10, 10);
        var pf = NewPathfinder(map);

        var result = pf.FindPath(new Point(0, 0), new Point(5, 0));

        result.Success.Should().BeTrue();
        result.Waypoints.Should().HaveCount(6);
        result.Cost.Should().BeApproximately(5.0f, 0.0001f);
        result.Waypoints[0].Should().Be(new Point(0, 0));
        result.Waypoints[^1].Should().Be(new Point(5, 0));
    }

    [Fact]
    public void LShape_ObstaclesForceDetour()
    {
        var map = new StubTilemap(10, 10);
        // Wall column at x=3 blocks direct diagonal path.
        for (int y = 0; y <= 4; y++) map.SetWall(3, y);
        var pf = NewPathfinder(map);

        var result = pf.FindPath(new Point(0, 0), new Point(5, 5));

        result.Success.Should().BeTrue();
        result.Waypoints[0].Should().Be(new Point(0, 0));
        result.Waypoints[^1].Should().Be(new Point(5, 5));
        // Must go around the wall; cost strictly greater than unobstructed octile.
        float unobstructed = AStarPathfinder.Octile(new Point(0, 0), new Point(5, 5));
        result.Cost.Should().BeGreaterThan(unobstructed);
    }

    [Fact]
    public void NoPath_ReturnsFailureWithNoPathReason()
    {
        var map = new StubTilemap(5, 5);
        // Seal (2,*) column to fully isolate (4,4).
        for (int y = 0; y < 5; y++) map.SetWall(2, y);
        var pf = NewPathfinder(map);

        var result = pf.FindPath(new Point(0, 0), new Point(4, 4));

        result.Success.Should().BeFalse();
        result.Reason.Should().Be(PathFailReason.NoPath);
    }

    [Fact]
    public void SameTile_ReturnsSingleWaypoint()
    {
        var map = new StubTilemap(5, 5);
        var pf = NewPathfinder(map);

        var result = pf.FindPath(new Point(2, 2), new Point(2, 2));

        result.Success.Should().BeTrue();
        result.Waypoints.Should().HaveCount(1);
        result.Waypoints[0].Should().Be(new Point(2, 2));
        result.Cost.Should().Be(0f);
    }

    [Fact]
    public void DiagonalCornerCut_Prohibited()
    {
        var map = new StubTilemap(3, 3);
        map.SetWall(1, 0);
        map.SetWall(0, 1);
        var pf = NewPathfinder(map);

        // (0,0)→(1,1) must fail: both cardinals of the diagonal are walls.
        var result = pf.FindPath(new Point(0, 0), new Point(1, 1));

        result.Success.Should().BeFalse();
        result.Reason.Should().Be(PathFailReason.NoPath);
    }

    [Fact]
    public void Determinism_SameSeedSamePath()
    {
        var map1 = new StubTilemap(10, 10);
        var map2 = new StubTilemap(10, 10);
        var pf1 = NewPathfinder(map1, seed: 123);
        var pf2 = NewPathfinder(map2, seed: 123);

        var r1 = pf1.FindPath(new Point(0, 0), new Point(9, 9));
        var r2 = pf2.FindPath(new Point(0, 0), new Point(9, 9));

        r1.Success.Should().BeTrue();
        r2.Success.Should().BeTrue();
        r1.Waypoints.Should().Equal(r2.Waypoints);
        r1.Cost.Should().Be(r2.Cost);
    }

    [Fact]
    public void OctileHeuristic_IsAdmissible()
    {
        var map = new StubTilemap(20, 20);
        var pf = NewPathfinder(map);

        // Over many goals, h(start, goal) must never exceed actual cost.
        for (int gx = 0; gx < 20; gx += 3)
        {
            for (int gy = 0; gy < 20; gy += 3)
            {
                var start = new Point(0, 0);
                var goal = new Point(gx, gy);
                float h = AStarPathfinder.Octile(start, goal);
                var r = pf.FindPath(start, goal);
                r.Success.Should().BeTrue();
                h.Should().BeLessThanOrEqualTo(r.Cost + 0.0001f,
                    because: $"h={h} must not exceed actual {r.Cost} for {goal}");
            }
        }
    }
}
