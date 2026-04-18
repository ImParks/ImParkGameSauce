using ImPark.Shared.Geometry;

namespace ImPark.Core.Pathfinding.Domain;

public readonly record struct PathRequest(
    int EntityId,
    Point Start,
    Point Goal,
    int Priority,
    Action<PathResult> Callback,
    ITraverseParms? Parms
);
