using System.Collections.Generic;
using FluentAssertions;
using ImPark.Core.Time;
using ImPark.Pawn.Application.Needs;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Defs;
using ImPark.Pawn.Domain.Events;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using Xunit;

namespace ImPark.Pawn.Tests.Needs;

public sealed class NeedsTickSystemTests
{
    private readonly World world = new();
    private readonly EventBus bus = new();
    private readonly StubDefDatabase defs = new();

    private EntityId SpawnHungryPawn(float startingLevel = 1f)
    {
        var id = world.CreateEntity();
        world.AddComponent(id, new PawnTag());
        world.AddComponent(id, new HungerComponent { Level = startingLevel });
        return id;
    }

    [Fact]
    public void Hunger_Decreases_Over_Time()
    {
        // FallPerDay = 1.0 -> fall per second = 1/86400. Run 8640 seconds => 0.1 drop.
        defs.Add(new NeedDef
        {
            DefId = NeedsTickSystem.HungerDefId,
            FallPerDay = 1.0f,
            ThreshPercentByLevel = new[] { 0.15f, 0.35f, 0.7f },
        });

        var id = SpawnHungryPawn(1.0f);
        using var system = new NeedsTickSystem(world, bus, defs);

        bus.PublishSync(new TimeTickEvent(1, 8640f, TickPhase.AI));

        ref var h = ref world.GetComponent<HungerComponent>(id);
        h.Level.Should().BeApproximately(0.9f, 1e-4f);
    }

    [Fact]
    public void Hunger_Hits_Threshold_Emits_NeedCriticalEvent()
    {
        defs.Add(new NeedDef
        {
            DefId = NeedsTickSystem.HungerDefId,
            FallPerDay = 1.0f,
            ThreshPercentByLevel = new[] { 0.15f, 0.35f, 0.7f },
        });

        var id = SpawnHungryPawn(0.72f);
        using var system = new NeedsTickSystem(world, bus, defs);

        var captured = new List<NeedCriticalEvent>();
        using var sub = bus.Subscribe<NeedCriticalEvent>(e => captured.Add(e));

        // Drop just past the 0.7 threshold: from 0.72 -> ~0.62.
        bus.PublishSync(new TimeTickEvent(1, 8640f, TickPhase.AI));

        captured.Should().ContainSingle();
        captured[0].EntityId.Should().Be(id.Value);
        captured[0].NeedDefId.Should().Be(NeedsTickSystem.HungerDefId);
        captured[0].Level.Should().BeLessThan(0.7f);
    }

    [Fact]
    public void Disabled_Need_Does_Not_Decrement()
    {
        defs.Add(new NeedDef
        {
            DefId = NeedsTickSystem.HungerDefId,
            FallPerDay = 1.0f,
            Disabled = true,
        });

        var id = SpawnHungryPawn(0.5f);
        using var system = new NeedsTickSystem(world, bus, defs);

        bus.PublishSync(new TimeTickEvent(1, 86400f, TickPhase.AI));

        ref var h = ref world.GetComponent<HungerComponent>(id);
        h.Level.Should().Be(0.5f);
    }

    [Fact]
    public void Level_Clamped_To_Zero_Floor()
    {
        defs.Add(new NeedDef
        {
            DefId = NeedsTickSystem.HungerDefId,
            FallPerDay = 1.0f,
        });

        var id = SpawnHungryPawn(0.05f);
        using var system = new NeedsTickSystem(world, bus, defs);

        // Burn through a full day in one tick - would fall by 1.0, clamped at 0.
        bus.PublishSync(new TimeTickEvent(1, 86400f, TickPhase.AI));

        ref var h = ref world.GetComponent<HungerComponent>(id);
        h.Level.Should().Be(0f);
    }

    [Fact]
    public void NonAi_Phase_Is_Ignored()
    {
        defs.Add(new NeedDef
        {
            DefId = NeedsTickSystem.HungerDefId,
            FallPerDay = 1.0f,
        });

        var id = SpawnHungryPawn(1.0f);
        using var system = new NeedsTickSystem(world, bus, defs);

        bus.PublishSync(new TimeTickEvent(1, 86400f, TickPhase.Render));

        ref var h = ref world.GetComponent<HungerComponent>(id);
        h.Level.Should().Be(1f);
    }
}
