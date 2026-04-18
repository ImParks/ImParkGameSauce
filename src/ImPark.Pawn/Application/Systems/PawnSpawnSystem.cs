using System;
using System.Collections.Generic;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Defs;
using ImPark.Pawn.Domain.Events;
using ImPark.Shared.Defs;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;

namespace ImPark.Pawn.Application.Systems;

public sealed class PawnSpawnSystem : IDisposable
{
    private readonly World world;
    private readonly IEventBus eventBus;
    private readonly IDefDatabase defs;
    private readonly IDisposable subscription;

    public PawnSpawnSystem(World world, IEventBus eventBus, IDefDatabase defs)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        this.defs = defs ?? throw new ArgumentNullException(nameof(defs));

        subscription = eventBus.Subscribe<PawnSpawnRequestEvent>(OnSpawnRequest);
    }

    public void Dispose()
    {
        subscription.Dispose();
    }

    private void OnSpawnRequest(PawnSpawnRequestEvent evt)
    {
        var kind = defs.Get<PawnKindDef>(evt.PawnKindDefId);
        if (kind is null)
        {
            Console.Error.WriteLine(
                $"[PawnSpawnSystem] PawnKindDef '{evt.PawnKindDefId}' not found; spawn aborted.");
            return;
        }

        var thing = defs.Get<PawnThingDef>(kind.ThingDefId);
        if (thing is null)
        {
            Console.Error.WriteLine(
                $"[PawnSpawnSystem] PawnThingDef '{kind.ThingDefId}' not found for kind '{kind.DefId}'.");
            return;
        }

        var entity = world.CreateEntity();

        world.AddComponent(entity, new PawnTag());
        world.AddComponent(entity, new PawnPositionComponent
        {
            Position = evt.Position,
            MapId = evt.MapId,
        });
        world.AddComponent(entity, new PawnMoveSpeedComponent
        {
            TilesPerSecond = thing.RaceProps.BaseMoveSpeed,
        });
        world.AddComponent(entity, new WorkCapabilityComponent
        {
            CanConstruct = true,
            CanHaul = true,
            CanCraft = true,
            CanCook = true,
            CanGrow = true,
        });
        world.AddComponent(entity, new SkillsComponent
        {
            Skills = new Dictionary<string, SkillRecord>(),
        });
        world.AddComponent(entity, new DefRefComponent
        {
            DefId = kind.ThingDefId,
        });

        eventBus.PublishSync(new PawnSpawnedEvent(
            entity.Value,
            kind.ThingDefId,
            evt.Position));
    }
}
