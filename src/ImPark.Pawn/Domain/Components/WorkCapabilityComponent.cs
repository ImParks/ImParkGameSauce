using ImPark.Shared.ECS;

namespace ImPark.Pawn.Domain.Components;

public struct WorkCapabilityComponent : IComponent
{
    public bool CanConstruct;
    public bool CanHaul;
    public bool CanCraft;
    public bool CanCook;
    public bool CanGrow;
}
