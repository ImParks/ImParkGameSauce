using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.Events;

// Dispatched synchronously by BodyPartSystem when a non-critical part takes damage
// that does not destroy the part (or destroys a non-critical part).
[Event("pawn.wounded")]
public readonly record struct PawnWoundedEvent(
    long EntityId,
    string PartId,
    float Severity);
