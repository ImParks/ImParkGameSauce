using System.Collections.Generic;
using ImPark.Pawn.Application.WorkTypes;
using ImPark.Shared.ECS;
using ImPark.Shared.Geometry;

namespace ImPark.Pawn.Contracts;

public interface IPawnQuery
{
    EntityId? GetPawnById(EntityId id);

    IEnumerable<EntityId> GetIdlePawns(
        int mapId,
        WorkTypeFilter filter = WorkTypeFilter.None);

    IEnumerable<EntityId> GetPawnsInRadius(
        Point center,
        float radius,
        int mapId,
        WorkTypeFilter filter = WorkTypeFilter.None);
}
