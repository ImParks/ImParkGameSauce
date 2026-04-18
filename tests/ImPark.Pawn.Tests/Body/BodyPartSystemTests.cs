using System;
using System.Collections.Generic;
using FluentAssertions;
using ImPark.Pawn.Application.Body;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Defs;
using ImPark.Pawn.Domain.Events;
using ImPark.Shared.Defs;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using Xunit;

namespace ImPark.Pawn.Tests.Body;

public sealed class BodyPartSystemTests
{
    private const float RaceBaseHp = 100f;

    private readonly World world = new();
    private readonly EventBus bus = new();
    private readonly StubDefDatabase defs = new();

    public BodyPartSystemTests()
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
                    new BodyPartNode { PartId = "head", HpRatio = 0.6f, Critical = true },
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

    private BodyPartComponent GetPart(EntityId pawn, string partId)
    {
        ref var body = ref world.GetComponent<BodyComponent>(pawn);
        foreach (var raw in body.PartEntityIds)
        {
            var e = new EntityId(raw);
            ref var bp = ref world.GetComponent<BodyPartComponent>(e);
            if (bp.PartId == partId) return bp;
        }
        throw new InvalidOperationException($"Part '{partId}' not found.");
    }

    [Fact]
    public void Damage_Reduces_Part_Hp()
    {
        var pawn = SpawnPawnWithBody();
        using var sys = new BodyPartSystem(world, bus);

        bus.PublishSync(new CombatDamageEvent(pawn.Value, "leftArm", 20f, "blunt"));

        var arm = GetPart(pawn, "leftArm");
        arm.CurrentHp.Should().BeApproximately(RaceBaseHp * 0.5f - 20f, 0.0001f);
    }

    [Fact]
    public void Damage_To_Critical_Part_Emits_PawnDiedEvent()
    {
        var pawn = SpawnPawnWithBody();
        using var sys = new BodyPartSystem(world, bus);

        PawnDiedEvent? died = null;
        bus.Subscribe<PawnDiedEvent>(e => died = e);

        // head MaxHp = 60; 100 damage wipes it out.
        bus.PublishSync(new CombatDamageEvent(pawn.Value, "head", 100f, "blunt"));

        died.Should().NotBeNull();
        died!.Value.EntityId.Should().Be(pawn.Value);
        died.Value.Cause.Should().Be("part_destroyed:head");

        ref var health = ref world.GetComponent<HealthComponent>(pawn);
        health.Dead.Should().BeTrue();
    }

    [Fact]
    public void Damage_To_NonCritical_Part_Emits_PawnWoundedEvent()
    {
        var pawn = SpawnPawnWithBody();
        using var sys = new BodyPartSystem(world, bus);

        PawnWoundedEvent? wounded = null;
        PawnDiedEvent? died = null;
        bus.Subscribe<PawnWoundedEvent>(e => wounded = e);
        bus.Subscribe<PawnDiedEvent>(e => died = e);

        bus.PublishSync(new CombatDamageEvent(pawn.Value, "leftArm", 10f, "blunt"));

        wounded.Should().NotBeNull();
        wounded!.Value.PartId.Should().Be("leftArm");
        wounded.Value.Severity.Should().Be(10f);
        died.Should().BeNull();
    }

    [Fact]
    public void Cut_Damage_Sets_BleedRate()
    {
        var pawn = SpawnPawnWithBody();
        using var sys = new BodyPartSystem(world, bus);

        bus.PublishSync(new CombatDamageEvent(pawn.Value, "leftArm", 10f, "cut"));

        var arm = GetPart(pawn, "leftArm");
        arm.BleedRate.Should().BeGreaterThan(0f);

        // Also applies to stab damage.
        bus.PublishSync(new CombatDamageEvent(pawn.Value, "leftArm", 5f, "stab"));
        var arm2 = GetPart(pawn, "leftArm");
        arm2.BleedRate.Should().BeGreaterThan(arm.BleedRate);
    }

    [Fact]
    public void PainTotal_Recalculated()
    {
        var pawn = SpawnPawnWithBody();
        using var sys = new BodyPartSystem(world, bus);

        ref var healthBefore = ref world.GetComponent<HealthComponent>(pawn);
        healthBefore.PainTotal.Should().Be(0f);

        bus.PublishSync(new CombatDamageEvent(pawn.Value, "leftArm", 10f, "blunt"));

        ref var healthAfter = ref world.GetComponent<HealthComponent>(pawn);
        healthAfter.PainTotal.Should().BeGreaterThan(0f);
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
