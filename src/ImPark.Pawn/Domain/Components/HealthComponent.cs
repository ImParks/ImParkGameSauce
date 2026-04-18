using ImPark.Shared.ECS;

namespace ImPark.Pawn.Domain.Components;

// Aggregate health state attached to the pawn entity (not to individual parts).
// PainTotal is recomputed by BodyPartSystem whenever part Pain values change.
public struct HealthComponent : IComponent
{
    public float PainTotal;
    public bool Downed;
    public bool Dead;
}
