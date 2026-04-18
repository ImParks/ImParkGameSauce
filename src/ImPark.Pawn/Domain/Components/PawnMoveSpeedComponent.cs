using ImPark.Shared.ECS;

namespace ImPark.Pawn.Domain.Components;

public struct PawnMoveSpeedComponent : IComponent
{
    public float TilesPerSecond;
}
