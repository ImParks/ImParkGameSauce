using System.Collections.Generic;
using FluentAssertions;
using ImPark.Pawn.Application.Needs.Services;
using ImPark.Pawn.Domain.Components;
using ImPark.Pawn.Domain.Defs;
using ImPark.Pawn.Domain.Events;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using Xunit;

namespace ImPark.Pawn.Tests.Needs;

public sealed class ThoughtServiceTests
{
    private readonly World world = new();
    private readonly EventBus bus = new();
    private readonly StubDefDatabase defs = new();

    private EntityId SpawnPawnWithThoughts()
    {
        var id = world.CreateEntity();
        world.AddComponent(id, new PawnTag());
        world.AddComponent(id, new ThoughtsComponent
        {
            Thoughts = new List<ActiveThought>(),
        });
        return id;
    }

    [Fact]
    public void AddThought_Creates_ActiveThought_With_Duration()
    {
        defs.Add(new ThoughtDef
        {
            DefId = "Compliment",
            Offset = 0.05f,
            DurationTicks = 1234f,
            Stackable = false,
        });

        var id = SpawnPawnWithThoughts();
        var service = new ThoughtService(world, bus, defs);

        service.AddThought(id.Value, "Compliment").Should().BeTrue();

        var thoughts = world.GetComponent<ThoughtsComponent>(id).Thoughts;
        thoughts.Should().ContainSingle();
        thoughts[0].DefId.Should().Be("Compliment");
        thoughts[0].Offset.Should().Be(0.05f);
        thoughts[0].RemainingTicks.Should().Be(1234f);
    }

    [Fact]
    public void NonStackable_Thought_Resets_Duration_Not_Duplicate()
    {
        defs.Add(new ThoughtDef
        {
            DefId = "Annoyed",
            Offset = -0.05f,
            DurationTicks = 1000f,
            Stackable = false,
        });

        var id = SpawnPawnWithThoughts();
        var service = new ThoughtService(world, bus, defs);

        service.AddThought(id.Value, "Annoyed").Should().BeTrue();

        // Simulate some decay before re-adding.
        var list = world.GetComponent<ThoughtsComponent>(id).Thoughts;
        var existing = list[0];
        existing.RemainingTicks = 200f;
        list[0] = existing;

        service.AddThought(id.Value, "Annoyed").Should().BeTrue();

        list = world.GetComponent<ThoughtsComponent>(id).Thoughts;
        list.Should().ContainSingle();
        list[0].RemainingTicks.Should().Be(1000f);
    }

    [Fact]
    public void Stackable_Thought_Duplicates()
    {
        defs.Add(new ThoughtDef
        {
            DefId = "Wound",
            Offset = -0.02f,
            DurationTicks = 500f,
            Stackable = true,
        });

        var id = SpawnPawnWithThoughts();
        var service = new ThoughtService(world, bus, defs);

        service.AddThought(id.Value, "Wound");
        service.AddThought(id.Value, "Wound");
        service.AddThought(id.Value, "Wound");

        var list = world.GetComponent<ThoughtsComponent>(id).Thoughts;
        list.Should().HaveCount(3);
        list.Should().OnlyContain(t => t.DefId == "Wound");
    }

    [Fact]
    public void Emits_ThoughtAddedEvent()
    {
        defs.Add(new ThoughtDef
        {
            DefId = "Praise",
            Offset = 0.1f,
            DurationTicks = 600f,
            Stackable = false,
        });

        var id = SpawnPawnWithThoughts();
        var service = new ThoughtService(world, bus, defs);

        var captured = new List<ThoughtAddedEvent>();
        using var sub = bus.Subscribe<ThoughtAddedEvent>(e => captured.Add(e));

        service.AddThought(id.Value, "Praise");
        bus.Flush();

        captured.Should().ContainSingle();
        captured[0].EntityId.Should().Be(id.Value);
        captured[0].ThoughtDefId.Should().Be("Praise");
    }
}
