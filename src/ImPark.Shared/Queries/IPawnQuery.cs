using ImPark.Shared.ECS;

namespace ImPark.Shared.Queries;

public readonly record struct PawnData(
    EntityId Id,
    string DefId,
    Vec2 Position,
    bool IsIdle
);

public interface IPawnQuery
{
    PawnData? GetPawnById(EntityId id);
    IReadOnlyList<EntityId> GetIdlePawns();
    IReadOnlyList<EntityId> GetPawnsInRadius(Vec2 center, float radius);
}
