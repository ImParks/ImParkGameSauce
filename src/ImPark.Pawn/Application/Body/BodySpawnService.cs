using System;
using System.Collections.Generic;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Defs;
using ImPark.Shared.Defs;
using ImPark.Shared.ECS;

namespace ImPark.Pawn.Application.Body;

// Creates the body-part entity graph for a pawn from a BodyDef.
//
// Typical flow (caller responsibility):
//  1. PawnSpawnSystem creates the pawn entity and resolves its PawnThingDef.
//  2. Caller invokes BodySpawnService.CreateBody(pawnId, raceProps.BodyDefId).
//  3. This service reads the BodyDef, recursively instantiates one child
//     entity per BodyPartNode, and attaches BodyComponent + HealthComponent
//     to the pawn.
//
// Phase 2 note: medical module is OFF, so HediffDef.Stages progression is
// not wired. Parts start at full HP with zero bleed/pain.
public sealed class BodySpawnService
{
    // Default race HP scalar used when no explicit BaseHealthScale is set.
    // Encoded here (not a magic number - default constant for callers that
    // pass 0 or negative scale).
    private const float DefaultRaceBaseHp = 50f;

    private readonly World world;
    private readonly IDefDatabase defs;

    public BodySpawnService(World world, IDefDatabase defs)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.defs = defs ?? throw new ArgumentNullException(nameof(defs));
    }

    // Creates the body graph for the given pawn entity using the named BodyDef.
    // raceBaseHp scales every part's MaxHp via node.HpRatio. A value <= 0
    // falls back to DefaultRaceBaseHp.
    public void CreateBody(long pawnEntityId, string bodyDefId, float raceBaseHp = DefaultRaceBaseHp)
    {
        var bodyDef = defs.Get<BodyDef>(bodyDefId);
        if (bodyDef is null)
        {
            Console.Error.WriteLine(
                $"[BodySpawnService] BodyDef '{bodyDefId}' not found; body not created for entity {pawnEntityId}.");
            return;
        }

        var pawn = new EntityId(pawnEntityId);
        if (!world.IsAlive(pawn))
        {
            Console.Error.WriteLine(
                $"[BodySpawnService] Pawn entity {pawnEntityId} is not alive; body not created.");
            return;
        }

        if (raceBaseHp <= 0f)
            raceBaseHp = DefaultRaceBaseHp;

        var partIds = new List<long>();
        CreatePartRecursive(bodyDef.Root, parentPartId: null, raceBaseHp, partIds);

        world.AddComponent(pawn, new BodyComponent
        {
            PartEntityIds = partIds,
            BodyDefId = bodyDefId,
        });

        world.AddComponent(pawn, new HealthComponent
        {
            PainTotal = 0f,
            Downed = false,
            Dead = false,
        });
    }

    private void CreatePartRecursive(
        BodyPartNode node,
        string? parentPartId,
        float raceBaseHp,
        List<long> partIds)
    {
        var partEntity = world.CreateEntity();

        var maxHp = raceBaseHp * node.HpRatio;
        world.AddComponent(partEntity, new BodyPartComponent
        {
            PartId = node.PartId,
            ParentPartId = parentPartId,
            MaxHp = maxHp,
            CurrentHp = maxHp,
            Critical = node.Critical,
            BleedRate = 0f,
            Pain = 0f,
        });

        partIds.Add(partEntity.Value);

        foreach (var child in node.Children)
            CreatePartRecursive(child, node.PartId, raceBaseHp, partIds);
    }
}
