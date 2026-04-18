using ImPark.Shared.ECS;

namespace ImPark.Pawn.Domain.Components;

// Body parts are modeled as child entities of a pawn.
// Each part entity holds a single BodyPartComponent. The parent pawn holds
// a BodyComponent that lists all part entity IDs.
public struct BodyPartComponent : IComponent
{
    public string PartId;
    public string? ParentPartId;
    public float MaxHp;
    public float CurrentHp;
    public bool Critical;
    public float BleedRate;
    public float Pain;
}
