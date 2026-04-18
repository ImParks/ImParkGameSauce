using ImPark.Shared.Events;
using ImPark.Shared.Geometry;

namespace ImPark.Pawn.Domain.Events;

[Event("pawn.spawned")]
public readonly record struct PawnSpawnedEvent(
    long EntityId,
    string DefId,
    Point Position);
