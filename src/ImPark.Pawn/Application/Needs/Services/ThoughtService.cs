using System;
using System.Collections.Generic;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Defs;
using ImPark.Pawn.Domain.Events;
using ImPark.Shared.Defs;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;

namespace ImPark.Pawn.Application.Needs.Services;

// Programmatic entry point for other systems (e.g. combat, social) to add
// thoughts to a pawn. Handles ThoughtDef lookup, non-stackable de-dup, and
// ThoughtAddedEvent emission.
public sealed class ThoughtService
{
    private readonly World world;
    private readonly IEventBus eventBus;
    private readonly IDefDatabase defs;

    public ThoughtService(World world, IEventBus eventBus, IDefDatabase defs)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        this.defs = defs ?? throw new ArgumentNullException(nameof(defs));
    }

    // Returns true when a thought was added or its duration reset; false if
    // the def/entity/ThoughtsComponent was missing.
    public bool AddThought(long entityId, string thoughtDefId)
    {
        if (string.IsNullOrEmpty(thoughtDefId)) return false;

        var def = defs.Get<ThoughtDef>(thoughtDefId);
        if (def is null) return false;

        var id = new EntityId(entityId);
        if (!world.IsAlive(id)) return false;

        if (!world.TryGetComponent<ThoughtsComponent>(id, out var thoughts) ||
            thoughts.Thoughts is null)
        {
            return false;
        }

        var list = thoughts.Thoughts;

        if (!def.Stackable)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].DefId == thoughtDefId)
                {
                    var reset = list[i];
                    reset.RemainingTicks = def.DurationTicks;
                    list[i] = reset;
                    eventBus.PublishAsync(new ThoughtAddedEvent(entityId, thoughtDefId));
                    return true;
                }
            }
        }

        list.Add(new ActiveThought
        {
            DefId = thoughtDefId,
            Offset = def.Offset,
            RemainingTicks = def.DurationTicks,
        });

        eventBus.PublishAsync(new ThoughtAddedEvent(entityId, thoughtDefId));
        return true;
    }
}
