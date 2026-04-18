using ImPark.Shared.Events;
using ImPark.Shared.Geometry;

namespace ImPark.Core.Pathfinding.Domain;

[Event("path.computed")]
public readonly record struct PathComputedEvent(
    ulong HandleId,
    int EntityId,
    Point Start,
    Point Goal,
    float Cost,
    int WaypointCount,
    bool Partial
);
