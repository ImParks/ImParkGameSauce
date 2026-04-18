using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.Events;

[Event("pawn.idle")]
public readonly record struct PawnIdleEvent(long EntityId);
