using ImPark.Shared.Events;
using ImPark.Shared.Geometry;

namespace ImPark.Core.Pathfinding.Domain;

[Event("path.invalidated")]
public readonly record struct PathInvalidatedEvent(
    ulong HandleId,
    int EntityId,
    Point TileChanged
);
