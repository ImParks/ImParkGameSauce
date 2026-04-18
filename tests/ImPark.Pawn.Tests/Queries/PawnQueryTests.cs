using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using ImPark.Pawn.Application.Queries;
using ImPark.Pawn.Application.WorkTypes;
using ImPark.Pawn.Domain.Components;
using ImPark.Shared.ECS;
using ImPark.Shared.Geometry;
using Xunit;

namespace ImPark.Pawn.Tests.Queries;

public sealed class PawnQueryTests
{
    private readonly World world = new();
    private readonly PawnQuery query;

    public PawnQueryTests()
    {
        query = new PawnQuery(world);
    }

    [Fact]
    public void GetPawnById_Returns_Null_When_Missing()
    {
        var fake = new EntityId(9999);
        query.GetPawnById(fake).Should().BeNull();
    }

    [Fact]
    public void GetPawnById_Returns_Id_When_Pawn_Exists()
    {
        var id = CreatePawn(new Point(0, 0), mapId: 0);
        query.GetPawnById(id).Should().Be(id);
    }

    [Fact]
    public void GetPawnById_Returns_Null_For_NonPawn_Entity()
    {
        var id = world.CreateEntity(); // no PawnTag
        query.GetPawnById(id).Should().BeNull();
    }

    [Fact]
    public void GetIdlePawns_Excludes_Busy_Pawns()
    {
        var idleA = CreatePawn(new Point(0, 0), mapId: 0);
        var busy = CreatePawn(new Point(1, 1), mapId: 0);
        var idleB = CreatePawn(new Point(2, 2), mapId: 0);

        world.AddComponent(busy, new PawnBusyComponent());

        var result = query.GetIdlePawns(mapId: 0).ToList();

        result.Should().Contain(new[] { idleA, idleB });
        result.Should().NotContain(busy);
    }

    [Fact]
    public void GetIdlePawns_Filters_By_MapId()
    {
        var mapZero = CreatePawn(new Point(0, 0), mapId: 0);
        var mapOne = CreatePawn(new Point(0, 0), mapId: 1);

        var result = query.GetIdlePawns(mapId: 0).ToList();

        result.Should().ContainSingle().Which.Should().Be(mapZero);
        result.Should().NotContain(mapOne);
    }

    [Fact]
    public void GetPawnsInRadius_Filters_By_Distance_And_WorkType()
    {
        var near = CreatePawn(new Point(1, 0), mapId: 0);
        var far = CreatePawn(new Point(50, 50), mapId: 0);
        var nearNoConstruct = CreatePawn(new Point(0, 1), mapId: 0,
            cap: new WorkCapabilityComponent
            {
                CanConstruct = false,
                CanHaul = true,
                CanCraft = true,
                CanCook = true,
                CanGrow = true,
            });

        var result = query.GetPawnsInRadius(
            new Point(0, 0), radius: 3f, mapId: 0,
            filter: WorkTypeFilter.Construct).ToList();

        result.Should().Contain(near);
        result.Should().NotContain(far);
        result.Should().NotContain(nearNoConstruct);
    }

    [Fact]
    public void GetPawnsInRadius_Filters_By_MapId()
    {
        var mapZero = CreatePawn(new Point(0, 0), mapId: 0);
        var mapOne = CreatePawn(new Point(0, 0), mapId: 1);

        var result = query.GetPawnsInRadius(
            new Point(0, 0), radius: 100f, mapId: 0).ToList();

        result.Should().ContainSingle().Which.Should().Be(mapZero);
        result.Should().NotContain(mapOne);
    }

    [Fact]
    public void FilterConstruct_Excludes_Pawns_Without_CanConstruct()
    {
        var builder = CreatePawn(new Point(0, 0), mapId: 0);
        var nonBuilder = CreatePawn(new Point(0, 0), mapId: 0,
            cap: new WorkCapabilityComponent
            {
                CanConstruct = false,
                CanHaul = true,
                CanCraft = true,
                CanCook = true,
                CanGrow = true,
            });

        var result = query.GetIdlePawns(mapId: 0, WorkTypeFilter.Construct).ToList();

        result.Should().Contain(builder);
        result.Should().NotContain(nonBuilder);
    }

    private EntityId CreatePawn(Point position, int mapId, WorkCapabilityComponent? cap = null)
    {
        var id = world.CreateEntity();
        world.AddComponent(id, new PawnTag());
        world.AddComponent(id, new PawnPositionComponent
        {
            Position = position,
            MapId = mapId,
        });
        world.AddComponent(id, cap ?? new WorkCapabilityComponent
        {
            CanConstruct = true,
            CanHaul = true,
            CanCraft = true,
            CanCook = true,
            CanGrow = true,
        });
        world.AddComponent(id, new SkillsComponent
        {
            Skills = new Dictionary<string, SkillRecord>(),
        });
        return id;
    }
}
