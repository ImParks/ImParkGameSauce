using ImPark.Shared.ECS;
using ImPark.Shared.Geometry;

namespace ImPark.Pawn.Domain.Components;

public struct PawnPositionComponent : IComponent
{
    public Point Position;
    public int MapId;
}
