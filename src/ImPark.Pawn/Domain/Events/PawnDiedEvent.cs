using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.Events;

// Minimal shape shared with Agent 2C (ReservationService auto-release).
// Dispatched synchronously so that all subscribers (ReservationService,
// BodyPartSystem, NeedsTickSystem, UI) observe the death within the same
// logical tick. Subscriber ordering: see BodyPartSystem header comment.
[Event("pawn.died")]
public readonly record struct PawnDiedEvent(long EntityId, string Cause);
