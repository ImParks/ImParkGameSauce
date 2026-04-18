using System;
using System.Collections.Generic;
using FluentAssertions;
using ImPark.Core.Time;
using ImPark.Pawn.Application.Body;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Defs;
using ImPark.Pawn.Domain.Events;
using ImPark.Shared.Defs;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using Xunit;

namespace ImPark.Pawn.Tests.Body;

public sealed class BleedingTickSystemTests
{
    private const float RaceBaseHp = 100f;

    private readonly World world = new();
    private readonly EventBus bus = new();
    private readonly StubDefDatabase defs = new();

    public BleedingTickSystemTests()
    {
        defs.Add(new BodyDef
        {
            DefId = "Humanoid",
            Root = new BodyPartNode
            {
                PartId = "torso",
                HpRatio = 1.0f,
                Critical = true,
                Children = new[]
                {
                    new BodyPartNode { PartId = "leftArm", HpRatio = 0.5f, Critical = false },
                },
            },
        });
    }

    private EntityId SpawnPawnWithBody()
    {
        var pawn = world.CreateEntity();
        var svc = new BodySpawnService(world, defs);
        svc.CreateBody(pawn.Value, "Humanoid", RaceBaseHp);
        return pawn;
    }

    private ref BodyPartComponent GetPartRef(EntityId pawn, string partId)
    {
        ref var body = ref world.GetComponent<BodyComponent>(pawn);
        foreach (var raw in body.PartEntityIds)
        {
            var e = new EntityId(raw);
            ref var bp = ref world.GetComponent<BodyPartComponent>(e);
            if (bp.PartId == partId) return ref bp;
        }
        throw new InvalidOperationException($"Part '{partId}' not found.");
    }

    [Fact]
    public void Bleeding_Over_Ticks_Reduces_Hp()
    {
        var pawn = SpawnPawnWithBody();
        using var sys = new BleedingTickSystem(world, bus);

        ref var arm = ref GetPartRef(pawn, "leftArm");
        arm.BleedRate = 2f;
        var initialHp = arm.CurrentHp;

        bus.PublishSync(new TimeTickEvent(Tick: 1, DeltaTime: 1f, Phase: TickPhase.Events));

        ref var armAfter = ref GetPartRef(pawn, "leftArm");
        armAfter.CurrentHp.Should().BeApproximately(initialHp - 2f, 0.0001f);
    }

    [Fact]
    public void Bleeding_To_Zero_Critical_Emits_PawnDiedEvent_Once()
    {
        var pawn = SpawnPawnWithBody();
        using var sys = new BleedingTickSystem(world, bus);

        ref var torso = ref GetPartRef(pawn, "torso");
        torso.CurrentHp = 1f;
        torso.BleedRate = 10f; // Will wipe it out in one tick.

        int diedCount = 0;
        string? cause = null;
        bus.Subscribe<PawnDiedEvent>(e =>
        {
            diedCount++;
            cause = e.Cause;
        });

        bus.PublishSync(new TimeTickEvent(Tick: 1, DeltaTime: 1f, Phase: TickPhase.Events));
        bus.PublishSync(new TimeTickEvent(Tick: 2, DeltaTime: 1f, Phase: TickPhase.Events));

        diedCount.Should().Be(1);
        cause.Should().Be("bleed_out:torso");

        ref var health = ref world.GetComponent<HealthComponent>(pawn);
        health.Dead.Should().BeTrue();
    }

    [Fact]
    public void NoBleedRate_Parts_Unchanged()
    {
        var pawn = SpawnPawnWithBody();
        using var sys = new BleedingTickSystem(world, bus);

        ref var arm = ref GetPartRef(pawn, "leftArm");
        var beforeHp = arm.CurrentHp;
        arm.BleedRate = 0f;

        bool bleedingEventFired = false;
        bus.Subscribe<PawnBleedingTickEvent>(_ => bleedingEventFired = true);

        bus.PublishSync(new TimeTickEvent(Tick: 1, DeltaTime: 1f, Phase: TickPhase.Events));
        bus.Flush();

        ref var armAfter = ref GetPartRef(pawn, "leftArm");
        armAfter.CurrentHp.Should().Be(beforeHp);
        bleedingEventFired.Should().BeFalse();
    }

    private sealed class StubDefDatabase : IDefDatabase
    {
        private readonly Dictionary<Type, Dictionary<string, Def>> buckets = new();

        public bool IsSealed => false;

        public void Add(Def def)
        {
            var type = def.GetType();
            if (!buckets.TryGetValue(type, out var bucket))
            {
                bucket = new Dictionary<string, Def>();
                buckets[type] = bucket;
            }
            bucket[def.DefId] = def;
        }

        public T? Get<T>(string defId) where T : Def
        {
            if (buckets.TryGetValue(typeof(T), out var bucket) &&
                bucket.TryGetValue(defId, out var def))
            {
                return (T)def;
            }
            return null;
        }

        public IReadOnlyList<T> AllOf<T>() where T : Def
        {
            if (!buckets.TryGetValue(typeof(T), out var bucket))
                return Array.Empty<T>();

            var list = new List<T>(bucket.Count);
            foreach (var def in bucket.Values)
                list.Add((T)def);
            return list;
        }
    }
}
