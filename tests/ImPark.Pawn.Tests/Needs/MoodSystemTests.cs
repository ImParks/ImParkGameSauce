using System.Collections.Generic;
using FluentAssertions;
using ImPark.Core.Time;
using ImPark.Pawn.Application.Needs;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Events;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using Xunit;

namespace ImPark.Pawn.Tests.Needs;

public sealed class MoodSystemTests
{
    private readonly World world = new();
    private readonly EventBus bus = new();
    private readonly StubDefDatabase defs = new();

    private EntityId SpawnPawn(float hunger, float sleep, List<ActiveThought>? thoughts = null)
    {
        var id = world.CreateEntity();
        world.AddComponent(id, new PawnTag());
        world.AddComponent(id, new HungerComponent { Level = hunger });
        world.AddComponent(id, new SleepComponent { Level = sleep });
        world.AddComponent(id, new MoodComponent { Current = 0f });
        world.AddComponent(id, new ThoughtsComponent
        {
            Thoughts = thoughts ?? new List<ActiveThought>(),
        });
        return id;
    }

    [Fact]
    public void Mood_Is_Average_Of_Needs_Plus_Thoughts()
    {
        var id = SpawnPawn(
            hunger: 0.6f,
            sleep: 0.4f,
            thoughts: new List<ActiveThought>
            {
                new() { DefId = "Happy", Offset = 0.1f, RemainingTicks = 100f },
            });

        using var system = new MoodSystem(world, bus, defs);
        bus.PublishSync(new TimeTickEvent(1, 1f, TickPhase.AI));

        ref var mood = ref world.GetComponent<MoodComponent>(id);
        // avg(0.6, 0.4) = 0.5; + 0.1 offset = 0.6
        mood.Current.Should().BeApproximately(0.6f, 1e-4f);
    }

    [Fact]
    public void Mood_Clamped_0_To_1()
    {
        var highId = SpawnPawn(1.0f, 1.0f, new List<ActiveThought>
        {
            new() { DefId = "Euphoria", Offset = 5f, RemainingTicks = 100f },
        });
        var lowId = SpawnPawn(0f, 0f, new List<ActiveThought>
        {
            new() { DefId = "Despair", Offset = -5f, RemainingTicks = 100f },
        });

        using var system = new MoodSystem(world, bus, defs);
        bus.PublishSync(new TimeTickEvent(1, 1f, TickPhase.AI));

        world.GetComponent<MoodComponent>(highId).Current.Should().Be(1f);
        world.GetComponent<MoodComponent>(lowId).Current.Should().Be(0f);
    }

    [Fact]
    public void Expired_Thought_Removed_And_Emits_ThoughtExpiredEvent()
    {
        var id = SpawnPawn(0.5f, 0.5f, new List<ActiveThought>
        {
            new() { DefId = "Fading", Offset = 0.1f, RemainingTicks = 5f },
            new() { DefId = "Lasting", Offset = 0.2f, RemainingTicks = 100f },
        });

        using var system = new MoodSystem(world, bus, defs);

        var expired = new List<ThoughtExpiredEvent>();
        using var sub = bus.Subscribe<ThoughtExpiredEvent>(e => expired.Add(e));

        // dt=10 -> Fading (5) goes to -5, Lasting (100) goes to 90.
        bus.PublishSync(new TimeTickEvent(1, 10f, TickPhase.AI));
        bus.Flush();

        expired.Should().ContainSingle();
        expired[0].EntityId.Should().Be(id.Value);
        expired[0].ThoughtDefId.Should().Be("Fading");

        var thoughts = world.GetComponent<ThoughtsComponent>(id).Thoughts;
        thoughts.Should().ContainSingle();
        thoughts[0].DefId.Should().Be("Lasting");
        thoughts[0].RemainingTicks.Should().BeApproximately(90f, 1e-4f);
    }
}
