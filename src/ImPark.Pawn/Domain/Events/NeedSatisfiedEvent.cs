using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.Events;

// Async: satisfaction notifications are informational (logs, UI, mood follow-ups).
[Event("need.satisfied")]
public readonly record struct NeedSatisfiedEvent(long EntityId, string NeedDefId);
