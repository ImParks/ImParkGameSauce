using FluentAssertions;
using ImPark.Core.Pathfinding.Application;
using ImPark.Core.Pathfinding.Domain;
using ImPark.Core.Tilemap;
using ImPark.Shared.Geometry;
using ImPark.Shared.Random;
using Xunit;

namespace ImPark.Core.Tests.Pathfinding;

public class PathCacheTests
{
    [Fact]
    public void EvictContaining_RemovesCachedPathIncludingTile()
    {
        var cache = new PathCache();
        var wps = new[] { new Point(0, 0), new Point(1, 0), new Point(2, 0) };
        var result = new PathResult(true, wps, 2.0f, false, null);

        cache.Put(new Point(0, 0), new Point(2, 0), parmsHash: 0, result);
        cache.TryGet(new Point(0, 0), new Point(2, 0), 0, out _).Should().BeTrue();

        int evicted = cache.EvictContaining(new Point(1, 0));

        evicted.Should().Be(1);
        cache.TryGet(new Point(0, 0), new Point(2, 0), 0, out _).Should().BeFalse();
    }

    [Fact]
    public void LruEviction_WhenFull_RemovesLeastRecentlyUsed()
    {
        var cache = new PathCache(capacity: 2);
        var r1 = new PathResult(true, new[] { new Point(0, 0) }, 0f, false, null);
        var r2 = new PathResult(true, new[] { new Point(1, 1) }, 0f, false, null);
        var r3 = new PathResult(true, new[] { new Point(2, 2) }, 0f, false, null);

        cache.Put(new Point(0, 0), new Point(0, 0), 0, r1);
        cache.Put(new Point(1, 1), new Point(1, 1), 0, r2);
        // Touch r1 so r2 becomes LRU.
        cache.TryGet(new Point(0, 0), new Point(0, 0), 0, out _);
        cache.Put(new Point(2, 2), new Point(2, 2), 0, r3);

        cache.TryGet(new Point(1, 1), new Point(1, 1), 0, out _).Should().BeFalse();
        cache.TryGet(new Point(0, 0), new Point(0, 0), 0, out _).Should().BeTrue();
        cache.TryGet(new Point(2, 2), new Point(2, 2), 0, out _).Should().BeTrue();
    }

    [Fact]
    public void TileChangedEvent_InvalidationHandler_EvictsCachedPath()
    {
        var bus = new StubEventBus();
        var map = new StubTilemap(10, 10);
        var rng = new XorShift32(1);
        var astar = new AStarPathfinder(map, rng);
        var reach = new RegionReachabilityChecker(map);
        var cache = new PathCache();
        var svc = new PathRequestService(astar, reach, cache, bus, map);
        using var handler = new PathInvalidationHandler(cache, svc, bus);

        var wps = new[] { new Point(0, 0), new Point(1, 0), new Point(2, 0) };
        cache.Put(new Point(0, 0), new Point(2, 0), 0,
            new PathResult(true, wps, 2.0f, false, null));

        bus.PublishSync(new TileChangedEvent(1, 0, TerrainType.Soil, TerrainType.Stone));

        cache.TryGet(new Point(0, 0), new Point(2, 0), 0, out _).Should().BeFalse();
    }
}
