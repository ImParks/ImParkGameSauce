using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using ImPark.Pawn.Application.Body;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Defs;
using ImPark.Shared.Defs;
using ImPark.Shared.ECS;
using Xunit;

namespace ImPark.Pawn.Tests.Body;

public sealed class BodySpawnServiceTests
{
    private const float RaceBaseHp = 100f;

    private readonly World world = new();
    private readonly StubDefDatabase defs = new();

    public BodySpawnServiceTests()
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
                    new BodyPartNode
                    {
                        PartId = "leftArm",
                        HpRatio = 0.5f,
                        Children = new[]
                        {
                            new BodyPartNode { PartId = "leftHand", HpRatio = 0.3f },
                        },
                    },
                    new BodyPartNode { PartId = "rightArm", HpRatio = 0.5f },
                },
            },
        });
    }

    [Fact]
    public void CreateBody_Recursively_Creates_Part_Entities()
    {
        var pawn = world.CreateEntity();
        var service = new BodySpawnService(world, defs);

        service.CreateBody(pawn.Value, "Humanoid", RaceBaseHp);

        ref var body = ref world.GetComponent<BodyComponent>(pawn);
        body.PartEntityIds.Should().HaveCount(5); // torso + head + leftArm + leftHand + rightArm

        var partIds = new List<string>();
        var parents = new Dictionary<string, string?>();
        foreach (var raw in body.PartEntityIds)
        {
            var e = new EntityId(raw);
            ref var bp = ref world.GetComponent<BodyPartComponent>(e);
            partIds.Add(bp.PartId);
            parents[bp.PartId] = bp.ParentPartId;
        }

        partIds.Should().BeEquivalentTo(new[] { "torso", "head", "leftArm", "leftHand", "rightArm" });
        parents["torso"].Should().BeNull();
        parents["head"].Should().Be("torso");
        parents["leftArm"].Should().Be("torso");
        parents["leftHand"].Should().Be("leftArm");
        parents["rightArm"].Should().Be("torso");
    }

    [Fact]
    public void CreateBody_Sets_BodyComponent_With_Correct_Part_Ids()
    {
        var pawn = world.CreateEntity();
        var service = new BodySpawnService(world, defs);

        service.CreateBody(pawn.Value, "Humanoid", RaceBaseHp);

        ref var body = ref world.GetComponent<BodyComponent>(pawn);
        body.BodyDefId.Should().Be("Humanoid");
        body.PartEntityIds.Should().OnlyHaveUniqueItems();

        foreach (var raw in body.PartEntityIds)
        {
            var partEntity = new EntityId(raw);
            world.IsAlive(partEntity).Should().BeTrue();
            world.HasComponent<BodyPartComponent>(partEntity).Should().BeTrue();
        }

        var head = body.PartEntityIds
            .Select(r => world.GetComponent<BodyPartComponent>(new EntityId(r)))
            .First(bp => bp.PartId == "head");

        head.MaxHp.Should().BeApproximately(RaceBaseHp * 0.6f, 0.0001f);
        head.CurrentHp.Should().Be(head.MaxHp);
        head.Critical.Should().BeTrue();
        head.BleedRate.Should().Be(0f);
        head.Pain.Should().Be(0f);
    }

    [Fact]
    public void CreateBody_Initializes_HealthComponent_Defaults()
    {
        var pawn = world.CreateEntity();
        var service = new BodySpawnService(world, defs);

        service.CreateBody(pawn.Value, "Humanoid", RaceBaseHp);

        world.HasComponent<HealthComponent>(pawn).Should().BeTrue();
        ref var health = ref world.GetComponent<HealthComponent>(pawn);

        health.Dead.Should().BeFalse();
        health.Downed.Should().BeFalse();
        health.PainTotal.Should().Be(0f);
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
