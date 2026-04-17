using ImPark.Shared.Events;

namespace ImPark.Core.Time;

[Event("time.tick")]
public readonly record struct TimeTickEvent(long Tick, float DeltaTime, TickPhase Phase);
