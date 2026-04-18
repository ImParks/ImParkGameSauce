using System;
using System.Collections.Generic;
using ImPark.Pawn.Application.WorkTypes;
using ImPark.Pawn.Contracts;
using ImPark.Pawn.Domain.Components;
using ImPark.Shared.ECS;
using ImPark.Shared.Geometry;

namespace ImPark.Pawn.Application.Queries;

public sealed class PawnQuery : IPawnQuery
{
    private readonly World world;

    public PawnQuery(World world)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public EntityId? GetPawnById(EntityId id)
    {
        if (!world.IsAlive(id)) return null;
        if (!world.HasComponent<PawnTag>(id)) return null;
        return id;
    }

    public IEnumerable<EntityId> GetIdlePawns(
        int mapId,
        WorkTypeFilter filter = WorkTypeFilter.None)
    {
        var matches = new List<EntityId>();
        var pawns = world.Query<PawnTag, PawnPositionComponent>();

        // Copy the snapshot so callers enumerating do not observe reuse of the shared query buffer.
        var snapshot = new EntityId[pawns.Count];
        pawns.CopyTo(snapshot, 0);

        for (int i = 0; i < snapshot.Length; i++)
        {
            var id = snapshot[i];
            if (world.HasComponent<PawnBusyComponent>(id)) continue;

            ref var pos = ref world.GetComponent<PawnPositionComponent>(id);
            if (pos.MapId != mapId) continue;

            if (!PassesWorkFilter(id, filter)) continue;

            matches.Add(id);
        }

        return matches;
    }

    public IEnumerable<EntityId> GetPawnsInRadius(
        Point center,
        float radius,
        int mapId,
        WorkTypeFilter filter = WorkTypeFilter.None)
    {
        var matches = new List<EntityId>();
        var pawns = world.Query<PawnTag, PawnPositionComponent>();

        var snapshot = new EntityId[pawns.Count];
        pawns.CopyTo(snapshot, 0);

        float r2 = radius * radius;

        for (int i = 0; i < snapshot.Length; i++)
        {
            var id = snapshot[i];
            ref var pos = ref world.GetComponent<PawnPositionComponent>(id);
            if (pos.MapId != mapId) continue;

            int dx = pos.Position.X - center.X;
            int dy = pos.Position.Y - center.Y;
            float dist2 = (float)(dx * dx + dy * dy);
            if (dist2 > r2) continue;

            if (!PassesWorkFilter(id, filter)) continue;

            matches.Add(id);
        }

        return matches;
    }

    private bool PassesWorkFilter(EntityId id, WorkTypeFilter filter)
    {
        if (filter == WorkTypeFilter.None) return true;
        if (!world.TryGetComponent<WorkCapabilityComponent>(id, out var cap)) return false;
        return WorkTypeFilterExtensions.Matches(cap, filter);
    }
}
