using ImPark.Shared.Events;
using ImPark.Shared.Geometry;

namespace ImPark.Core.Pathfinding.Domain;

[Event("path.requested")]
public readonly record struct PathRequestedEvent(
    ulong HandleId,
    int EntityId,
    Point Start,
    Point Goal,
    int Priority
);
