using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.Events;

[Event("pawn.despawned")]
public readonly record struct PawnDespawnedEvent(long EntityId);
