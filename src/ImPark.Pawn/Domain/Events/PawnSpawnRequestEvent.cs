using ImPark.Shared.Events;
using ImPark.Shared.Geometry;

namespace ImPark.Pawn.Domain.Events;

[Event("pawn.spawn-request")]
public readonly record struct PawnSpawnRequestEvent(
    string PawnKindDefId,
    Point Position,
    int MapId);
