using ImPark.Shared.ECS;

namespace ImPark.Pawn.Domain.Components;

// Level is normalized to [0, 1]; 1 = fully rested, 0 = exhausted.
public struct SleepComponent : IComponent
{
    public float Level;
}
