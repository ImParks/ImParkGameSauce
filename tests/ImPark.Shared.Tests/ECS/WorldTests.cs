using FluentAssertions;
using ImPark.Shared.ECS;
using Xunit;

namespace ImPark.Shared.Tests.ECS;

public struct Position : IComponent
{
    public float X;
    public float Y;
}

public struct Velocity : IComponent
{
    public float Dx;
    public float Dy;
}

public struct Health : IComponent
{
    public int Hp;
}

public class WorldTests
{
    [Fact]
    public void CreateEntity_ReturnsUniqueIds()
    {
        var world = new World();

        var e1 = world.CreateEntity();
        var e2 = world.CreateEntity();
        var e3 = world.CreateEntity();

        e1.Should().NotBe(e2);
        e2.Should().NotBe(e3);
        world.EntityCount.Should().Be(3);
    }

    [Fact]
    public void DestroyEntity_RemovesEntity()
    {
        var world = new World();
        var entity = world.CreateEntity();

        world.DestroyEntity(entity);

        world.IsAlive(entity).Should().BeFalse();
        world.EntityCount.Should().Be(0);
    }

    [Fact]
    public void DestroyEntity_AlsoRemovesComponents()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Position { X = 1, Y = 2 });
        world.AddComponent(entity, new Health { Hp = 100 });

        world.DestroyEntity(entity);

        world.IsAlive(entity).Should().BeFalse();
    }

    [Fact]
    public void DestroyEntity_ThrowsForDeadEntity()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.DestroyEntity(entity);

        var act = () => world.DestroyEntity(entity);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddComponent_And_GetComponent_Work()
    {
        var world = new World();
        var entity = world.CreateEntity();

        world.AddComponent(entity, new Position { X = 10, Y = 20 });

        ref var pos = ref world.GetComponent<Position>(entity);
        pos.X.Should().Be(10);
        pos.Y.Should().Be(20);
    }

    [Fact]
    public void GetComponent_ReturnsRef_AllowsMutation()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Position { X = 0, Y = 0 });

        ref var pos = ref world.GetComponent<Position>(entity);
        pos.X = 42;

        ref var posAgain = ref world.GetComponent<Position>(entity);
        posAgain.X.Should().Be(42);
    }

    [Fact]
    public void TryGetComponent_ReturnsTrueWhenPresent()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Position { X = 5, Y = 6 });

        var found = world.TryGetComponent<Position>(entity, out var pos);

        found.Should().BeTrue();
        pos.X.Should().Be(5);
    }

    [Fact]
    public void TryGetComponent_ReturnsFalseWhenMissing()
    {
        var world = new World();
        var entity = world.CreateEntity();

        var found = world.TryGetComponent<Position>(entity, out _);

        found.Should().BeFalse();
    }

    [Fact]
    public void RemoveComponent_Works()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Position { X = 1, Y = 2 });

        world.RemoveComponent<Position>(entity);

        world.HasComponent<Position>(entity).Should().BeFalse();
    }

    [Fact]
    public void RemoveComponent_ThrowsWhenMissing()
    {
        var world = new World();
        var entity = world.CreateEntity();

        var act = () => world.RemoveComponent<Position>(entity);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void HasComponent_ReturnsFalseForDeadEntity()
    {
        var world = new World();
        var entity = world.CreateEntity();
        world.AddComponent(entity, new Position { X = 1, Y = 2 });
        world.DestroyEntity(entity);

        world.HasComponent<Position>(entity).Should().BeFalse();
    }

    [Fact]
    public void Query_SingleType_ReturnsMatchingEntities()
    {
        var world = new World();
        var e1 = world.CreateEntity();
        var e2 = world.CreateEntity();
        var e3 = world.CreateEntity();

        world.AddComponent(e1, new Position { X = 1, Y = 1 });
        world.AddComponent(e2, new Position { X = 2, Y = 2 });
        world.AddComponent(e3, new Velocity { Dx = 1, Dy = 1 });

        var result = world.Query<Position>();

        result.Should().HaveCount(2);
        result.Should().Contain(e1);
        result.Should().Contain(e2);
        result.Should().NotContain(e3);
    }

    [Fact]
    public void Query_TwoTypes_ReturnsIntersection()
    {
        var world = new World();
        var e1 = world.CreateEntity();
        var e2 = world.CreateEntity();
        var e3 = world.CreateEntity();

        world.AddComponent(e1, new Position { X = 1, Y = 1 });
        world.AddComponent(e1, new Velocity { Dx = 1, Dy = 1 });
        world.AddComponent(e2, new Position { X = 2, Y = 2 });
        world.AddComponent(e3, new Velocity { Dx = 3, Dy = 3 });

        var result = world.Query<Position, Velocity>();

        result.Should().HaveCount(1);
        result.Should().Contain(e1);
    }

    [Fact]
    public void Query_ThreeTypes_ReturnsIntersection()
    {
        var world = new World();
        var e1 = world.CreateEntity();
        var e2 = world.CreateEntity();

        world.AddComponent(e1, new Position { X = 1, Y = 1 });
        world.AddComponent(e1, new Velocity { Dx = 1, Dy = 1 });
        world.AddComponent(e1, new Health { Hp = 100 });

        world.AddComponent(e2, new Position { X = 2, Y = 2 });
        world.AddComponent(e2, new Velocity { Dx = 2, Dy = 2 });

        var result = world.Query<Position, Velocity, Health>();

        result.Should().HaveCount(1);
        result.Should().Contain(e1);
    }

    [Fact]
    public void Query_EmptyStore_ReturnsEmptyList()
    {
        var world = new World();
        world.CreateEntity();

        var result = world.Query<Position>();

        result.Should().BeEmpty();
    }

    [Fact]
    public void SystemScheduler_ExecutesInOrderDeterministically()
    {
        var executionLog = new List<string>();

        var systemA = new TestSystem("A", 30, executionLog);
        var systemB = new TestSystem("B", 10, executionLog);
        var systemC = new TestSystem("C", 20, executionLog);

        var scheduler = new SystemScheduler();
        scheduler.Register(systemA);
        scheduler.Register(systemB);
        scheduler.Register(systemC);

        var world = new World();
        scheduler.UpdateAll(world, currentTick: 1);

        executionLog.Should().ContainInOrder("B", "C", "A");
    }

    [Fact]
    public void SystemScheduler_OrderIsStableAcrunsMultipleTicks()
    {
        var executionLog = new List<string>();

        var systemA = new TestSystem("A", 2, executionLog);
        var systemB = new TestSystem("B", 1, executionLog);

        var scheduler = new SystemScheduler();
        scheduler.Register(systemA);
        scheduler.Register(systemB);

        var world = new World();
        scheduler.UpdateAll(world, currentTick: 1);
        scheduler.UpdateAll(world, currentTick: 2);

        executionLog.Should().ContainInOrder("B", "A", "B", "A");
    }

    [Fact]
    public void StressTest_10kEntities()
    {
        const int ENTITY_COUNT = 10_000;
        var world = new World();
        var entities = new EntityId[ENTITY_COUNT];

        for (int i = 0; i < ENTITY_COUNT; i++)
        {
            entities[i] = world.CreateEntity();
            world.AddComponent(entities[i], new Position { X = i, Y = i * 2 });

            if (i % 2 == 0)
                world.AddComponent(entities[i], new Velocity { Dx = i, Dy = -i });
        }

        world.EntityCount.Should().Be(ENTITY_COUNT);

        var posOnly = world.Query<Position>();
        posOnly.Should().HaveCount(ENTITY_COUNT);

        var posAndVel = world.Query<Position, Velocity>();
        posAndVel.Should().HaveCount(ENTITY_COUNT / 2);

        ref var pos5000 = ref world.GetComponent<Position>(entities[5000]);
        pos5000.X.Should().Be(5000);

        for (int i = 0; i < ENTITY_COUNT; i += 3)
            world.DestroyEntity(entities[i]);

        int destroyed = (ENTITY_COUNT + 2) / 3;
        world.EntityCount.Should().Be(ENTITY_COUNT - destroyed);
    }

    private class TestSystem : ISystem
    {
        private readonly string name;
        private readonly List<string> log;

        public TestSystem(string name, int order, List<string> log)
        {
            this.name = name;
            Order = order;
            this.log = log;
        }

        public int Order { get; }

        public void Update(World world, long currentTick)
        {
            log.Add(name);
        }
    }
}
