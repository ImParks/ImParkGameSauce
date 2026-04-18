using ImPark.Shared.Geometry;
using ImPark.Shared.Queries;

namespace ImPark.Core.Pathfinding.Application;

// O(1) reachability via region flood-fill IDs. Two tiles are reachable
// iff they share a region id and both are walkable. Region id 0 is
// treated as "not assigned / impassable".
public sealed class RegionReachabilityChecker
{
    private readonly ITilemapQuery _tilemap;

    public RegionReachabilityChecker(ITilemapQuery tilemap)
    {
        _tilemap = tilemap;
    }

    public bool IsReachable(Point from, Point to)
    {
        if (!_tilemap.IsWalkable(from.X, from.Y)) return false;
        if (!_tilemap.IsWalkable(to.X, to.Y)) return false;

        int a = _tilemap.GetRegionId(from.X, from.Y);
        int b = _tilemap.GetRegionId(to.X, to.Y);

        if (a == 0 || b == 0) return false;
        return a == b;
    }
}
