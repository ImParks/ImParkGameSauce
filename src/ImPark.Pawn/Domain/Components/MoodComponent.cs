using ImPark.Shared.ECS;

namespace ImPark.Pawn.Domain.Components;

// Current mood normalized to [0, 1]. Recomputed by MoodSystem each tick from
// the pawn's needs average plus the sum of active thought offsets (clamped).
public struct MoodComponent : IComponent
{
    public float Current;
}
