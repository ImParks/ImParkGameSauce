using ImPark.Shared.ECS;

namespace ImPark.Pawn.Domain.Components;

// Level is normalized to [0, 1]; 1 = fully sated, 0 = starving.
public struct HungerComponent : IComponent
{
    public float Level;
}
