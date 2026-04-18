using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.Events;

// Async: informational - mood recompute happens in MoodSystem regardless.
[Event("thought.added")]
public readonly record struct ThoughtAddedEvent(long EntityId, string ThoughtDefId);
