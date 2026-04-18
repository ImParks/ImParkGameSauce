using ImPark.Shared.Events;
using ImPark.Shared.Geometry;

namespace ImPark.Core.Pathfinding.Domain;

[Event("path.failed")]
public readonly record struct PathFailedEvent(
    ulong HandleId,
    int EntityId,
    Point Start,
    Point Goal,
    PathFailReason Reason
);
