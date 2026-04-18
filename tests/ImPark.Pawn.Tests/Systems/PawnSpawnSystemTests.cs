using System;
using System.Collections.Generic;
using FluentAssertions;
using ImPark.Pawn.Application.Systems;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Defs;
using ImPark.Pawn.Domain.Events;
using ImPark.Shared.Defs;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using ImPark.Shared.Geometry;
using Xunit;

namespace ImPark.Pawn.Tests.Systems;

public sealed class PawnSpawnSystemTests
{
    private readonly World world = new();
    private readonly EventBus bus = new();
    private readonly StubDefDatabase defs = new();

    public PawnSpawnSystemTests()
    {
        defs.Add(new PawnThingDef
        {
            DefId = "Human",
            RaceProps = new RaceProps { BaseMoveSpeed = 4.5f },
        });
        defs.Add(new PawnKindDef
        {
            DefId = "Colonist",
            DefaultName = "Colonist",
            ThingDefId = "Human",
        });
    }

    [Fact]
    public void SpawnRequest_CreatesEntity_WithExpectedComponents()
    {
        using var system = new PawnSpawnSystem(world, bus, defs);

        bus.PublishSync(new PawnSpawnRequestEvent(
            "Colonist", new Point(5, 7), mapId: 1));

        world.EntityCount.Should().Be(1);

        // There must be exactly one pawn entity with all the spawn-time components.
        var pawns = world.Query<PawnTag, PawnPositionComponent>();
        pawns.Should().HaveCount(1);

        var id = pawns[0];
        world.HasComponent<PawnTag>(id).Should().BeTrue();
        world.HasComponent<PawnPositionComponent>(id).Should().BeTrue();
        world.HasComponent<PawnMoveSpeedComponent>(id).Should().BeTrue();
        world.HasComponent<WorkCapabilityComponent>(id).Should().BeTrue();
        world.HasComponent<SkillsComponent>(id).Should().BeTrue();
        world.HasComponent<DefRefComponent>(id).Should().BeTrue();

        ref var pos = ref world.GetComponent<PawnPositionComponent>(id);
        pos.Position.Should().Be(new Point(5, 7));
        pos.MapId.Should().Be(1);

        ref var speed = ref world.GetComponent<PawnMoveSpeedComponent>(id);
        speed.TilesPerSecond.Should().Be(4.5f);

        ref var cap = ref world.GetComponent<WorkCapabilityComponent>(id);
        cap.CanConstruct.Should().BeTrue();
        cap.CanHaul.Should().BeTrue();
        cap.CanCraft.Should().BeTrue();
        cap.CanCook.Should().BeTrue();
        cap.CanGrow.Should().BeTrue();

        ref var skills = ref world.GetComponent<SkillsComponent>(id);
        skills.Skills.Should().NotBeNull();
        skills.Skills.Should().BeEmpty();

        ref var defRef = ref world.GetComponent<DefRefComponent>(id);
        defRef.DefId.Should().Be("Human");
    }

    [Fact]
    public void SpawnEmits_PawnSpawnedEvent()
    {
        using var system = new PawnSpawnSystem(world, bus, defs);

        PawnSpawnedEvent? captured = null;
        bus.Subscribe<PawnSpawnedEvent>(e => captured = e);

        bus.PublishSync(new PawnSpawnRequestEvent(
            "Colonist", new Point(1, 2), mapId: 0));

        captured.Should().NotBeNull();
        captured!.Value.DefId.Should().Be("Human");
        captured.Value.Position.Should().Be(new Point(1, 2));
        captured.Value.EntityId.Should().BeGreaterThan(0);
    }

    [Fact]
    public void SpawnRequest_WithUnknownKind_DoesNotCreateEntity()
    {
        using var system = new PawnSpawnSystem(world, bus, defs);

        bus.PublishSync(new PawnSpawnRequestEvent(
            "Nonexistent", new Point(0, 0), mapId: 0));

        world.EntityCount.Should().Be(0);
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
