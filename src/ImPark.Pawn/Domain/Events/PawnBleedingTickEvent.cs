using ImPark.Shared.Events;

namespace ImPark.Pawn.Domain.Events;

// Internal notification published asynchronously by BleedingTickSystem after
// a pawn loses HP due to bleeding on the current tick. Consumers (UI, logs)
// may read it off the async queue without blocking the tick.
[Event("pawn.bleeding-tick")]
public readonly record struct PawnBleedingTickEvent(
    long EntityId,
    float AmountLost);
