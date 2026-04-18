using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.Events;

// Sync: consumers may want to react in the same tick (e.g. push a job).
[Event("need.critical")]
public readonly record struct NeedCriticalEvent(long EntityId, string NeedDefId, float Level);
