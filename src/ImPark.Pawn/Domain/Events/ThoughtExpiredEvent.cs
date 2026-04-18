using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.Events;

// Async: informational - emitted when RemainingTicks reaches 0 and the
// thought is removed from ThoughtsComponent.Thoughts.
[Event("thought.expired")]
public readonly record struct ThoughtExpiredEvent(long EntityId, string ThoughtDefId);
