using System;
using System.Collections.Generic;
using ImPark.Core.Time;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Events;
using ImPark.Shared.Defs;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;

namespace ImPark.Pawn.Application.Needs;

// Recomputes MoodComponent per pawn each AI tick:
//   mood = avg(needs) + sum(active thought offsets)  -> clamped to [0, 1]
// Also decays ActiveThought.RemainingTicks and removes expired entries,
// emitting ThoughtExpiredEvent (async) for each removal.
public sealed class MoodSystem : ISystem, IDisposable
{
    public int Order => 210;

    private readonly World world;
    private readonly IEventBus eventBus;
    private readonly IDisposable subscription;

    // Scratch list reused across ticks to avoid per-frame allocation when
    // filtering expired thoughts.
    private readonly List<ActiveThought> scratch = new();

    public MoodSystem(World world, IEventBus eventBus, IDefDatabase defs)
    {
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _ = defs; // kept in signature for symmetry with other need systems
        subscription = eventBus.Subscribe<TimeTickEvent>(OnTimeTick);
    }

    public void Dispose()
    {
        subscription.Dispose();
    }

    public void Update(World world, long currentTick)
    {
    }

    private void OnTimeTick(TimeTickEvent evt)
    {
        if (evt.Phase != TickPhase.AI) return;

        var entities = world.Query<PawnTag, MoodComponent>();
        // Snapshot entity ids because the query buffer is reused internally.
        var ids = new long[entities.Count];
        for (int i = 0; i < entities.Count; i++) ids[i] = entities[i].Value;

        for (int i = 0; i < ids.Length; i++)
        {
            var id = new EntityId(ids[i]);

            var needsSum = 0f;
            var needsCount = 0;
            if (world.TryGetComponent<HungerComponent>(id, out var hunger))
            {
                needsSum += hunger.Level;
                needsCount++;
            }
            if (world.TryGetComponent<SleepComponent>(id, out var sleep))
            {
                needsSum += sleep.Level;
                needsCount++;
            }

            var needsAvg = needsCount > 0 ? needsSum / needsCount : 0f;
            var offsetSum = DecayAndSumThoughts(id, evt.DeltaTime);

            ref var mood = ref world.GetComponent<MoodComponent>(id);
            mood.Current = Math.Clamp(needsAvg + offsetSum, 0f, 1f);
        }
    }

    // Decays RemainingTicks for all thoughts on the entity, removes any that
    // have expired (emitting ThoughtExpiredEvent), and returns the sum of
    // still-active offsets.
    private float DecayAndSumThoughts(EntityId id, float dt)
    {
        if (!world.TryGetComponent<ThoughtsComponent>(id, out var thoughts) ||
            thoughts.Thoughts is null)
        {
            return 0f;
        }

        var list = thoughts.Thoughts;
        scratch.Clear();

        var offsetSum = 0f;
        for (int j = 0; j < list.Count; j++)
        {
            var t = list[j];
            t.RemainingTicks -= dt;
            if (t.RemainingTicks <= 0f)
            {
                eventBus.PublishAsync(new ThoughtExpiredEvent(id.Value, t.DefId));
                continue;
            }
            offsetSum += t.Offset;
            scratch.Add(t);
        }

        if (scratch.Count != list.Count)
        {
            list.Clear();
            list.AddRange(scratch);
        }
        else
        {
            // No removals; just write back updated RemainingTicks values.
            for (int j = 0; j < scratch.Count; j++) list[j] = scratch[j];
        }

        return offsetSum;
    }
}
