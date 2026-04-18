using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.AI.Events;

// Async: fire-and-forget notification so logging/UI can react without blocking AI tick.
[Event("ai.job-started")]
public readonly record struct AIJobStartedEvent(long EntityId, string JobId, string JobDefId);
