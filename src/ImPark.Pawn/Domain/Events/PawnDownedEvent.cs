using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.Events;

[Event("pawn.downed")]
public readonly record struct PawnDownedEvent(long EntityId);
