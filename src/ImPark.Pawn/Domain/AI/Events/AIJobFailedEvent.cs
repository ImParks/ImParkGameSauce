using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.AI.Events;

// Sync: reservations must be released before the next AI tick picks a new job.
[Event("ai.job-failed")]
public readonly record struct AIJobFailedEvent(long EntityId, string JobId, string Reason);
