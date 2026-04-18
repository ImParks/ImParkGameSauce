using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.AI.Events;

// Async: downstream systems (stats, XP, UI) react after the fact.
[Event("ai.job-completed")]
public readonly record struct AIJobCompletedEvent(long EntityId, string JobId);
