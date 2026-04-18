using ImPark.Shared.Geometry;

namespace ImPark.Core.Pathfinding.Domain;

public readonly record struct PathResult(
    bool Success,
    Point[] Waypoints,
    float Cost,
    bool Partial,
    PathFailReason? Reason
);
