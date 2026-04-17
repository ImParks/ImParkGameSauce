using FluentAssertions;
using ImPark.Core.Modules;
using ImPark.Shared.Defs;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using ImPark.Shared.Modules;
using ImPark.Shared.Queries;
using Xunit;

namespace ImPark.Core.Tests.Modules;

internal sealed class StubTilemapQuery : ITilemapQuery
{
    public TileData GetTile(int x, int y) => new("stub", true, 0, 1.0f);
    public bool IsWalkable(int x, int y) => true;
    public int GetRegionId(int x, int y) => 0;
}

internal sealed class StubTimeQuery : ITimeQuery
{
    public long GetCurrentTick() => 0;
    public GameSpeed GetSpeed() => GameSpeed.Normal;
    public bool IsPaused() => false;
}

internal sealed class TestModule : IGameModule
{
    private readonly QueryRegistry? sharedRegistry;
    private readonly double value;

    public TestModule(string id, IReadOnlyList<string>? deps = null, QueryRegistry? sharedRegistry = null, double value = 42.0)
    {
        Id = id;
        Dependencies = deps ?? Array.Empty<string>();
        this.sharedRegistry = sharedRegistry;
        this.value = value;
    }

    public string Id { get; }
    public IReadOnlyList<string> Dependencies { get; }
    public int RegisterOrder { get; private set; } = -1;
    public int EnableOrder { get; private set; } = -1;
    public bool IsActive { get; private set; }
    public static int RegisterCounter;
    public static int EnableCounter;

    public void OnRegister(IModuleContext context)
    {
        RegisterOrder = Interlocked.Increment(ref RegisterCounter);
    }

    public void OnEnable()
    {
        EnableOrder = Interlocked.Increment(ref EnableCounter);
        IsActive = true;
        sharedRegistry?.Register<ITestQuery>(new RealTestQuery { Value = value }, RegistrationPolicy.Replace);
    }

    public void OnDisable()
    {
        IsActive = false;
        sharedRegistry?.Unregister<ITestQuery>();
    }
}

public class ModuleRegistryTests
{
    public ModuleRegistryTests()
    {
        TestModule.RegisterCounter = 0;
        TestModule.EnableCounter = 0;
    }

    private static IModuleContext CreateContext(IEventBus bus, IQueryRegistry queries) =>
        new ModuleContext(
            bus,
            new NullDefDatabase(),
            new World(),
            new StubTilemapQuery(),
            new StubTimeQuery(),
            queries);

    [Fact]
    public void Register_Adds_Module_In_Registered_State()
    {
        var reg = new ModuleRegistry(new EventBus());
        var mod = new TestModule("a");

        reg.Register(mod);

        reg.GetState("a").Should().Be(ModuleState.Registered);
        reg.IsEnabled("a").Should().BeFalse();
    }

    [Fact]
    public void Register_Duplicate_Throws()
    {
        var reg = new ModuleRegistry(new EventBus());
        reg.Register(new TestModule("a"));

        var act = () => reg.Register(new TestModule("a"));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ResolveAndEnable_Calls_OnRegister_Then_OnEnable_In_Topological_Order()
    {
        var reg = new ModuleRegistry(new EventBus());
        var queries = new QueryRegistry();
        // 'c' depends on 'b' which depends on 'a'
        var a = new TestModule("a");
        var b = new TestModule("b", new[] { "a" });
        var c = new TestModule("c", new[] { "b" });

        // Register out of order
        reg.Register(c);
        reg.Register(a);
        reg.Register(b);

        reg.ResolveAndEnable(CreateContext(new EventBus(), queries));

        a.RegisterOrder.Should().BeLessThan(b.RegisterOrder);
        b.RegisterOrder.Should().BeLessThan(c.RegisterOrder);
        a.EnableOrder.Should().BeLessThan(b.EnableOrder);
        b.EnableOrder.Should().BeLessThan(c.EnableOrder);
        // All OnRegister happened before any OnEnable
        c.RegisterOrder.Should().BeLessThan(a.EnableOrder);
    }

    [Fact]
    public void ResolveAndEnable_Detects_Cycle_And_Throws()
    {
        var reg = new ModuleRegistry(new EventBus());
        var a = new TestModule("a", new[] { "b" });
        var b = new TestModule("b", new[] { "a" });
        reg.Register(a);
        reg.Register(b);

        var act = () => reg.ResolveAndEnable(CreateContext(new EventBus(), new QueryRegistry()));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cycle*");
    }

    [Fact]
    public void ResolveAndEnable_Missing_Dependency_Throws()
    {
        var reg = new ModuleRegistry(new EventBus());
        reg.Register(new TestModule("a", new[] { "ghost" }));

        var act = () => reg.ResolveAndEnable(CreateContext(new EventBus(), new QueryRegistry()));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ghost*");
    }

    [Fact]
    public void ResolveAndEnable_Emits_ModuleLoadedEvent_Per_Module()
    {
        var bus = new EventBus();
        var loaded = new List<string>();
        bus.Subscribe<ModuleLoadedEvent>(e => loaded.Add(e.ModuleId));

        var reg = new ModuleRegistry(bus);
        reg.Register(new TestModule("a"));
        reg.Register(new TestModule("b", new[] { "a" }));

        reg.ResolveAndEnable(CreateContext(bus, new QueryRegistry()));

        loaded.Should().Equal("a", "b");
    }

    [Fact]
    public void Disable_Stops_Module_From_Being_Active()
    {
        var bus = new EventBus();
        var reg = new ModuleRegistry(bus);
        var mod = new TestModule("a");
        reg.Register(mod);
        reg.ResolveAndEnable(CreateContext(bus, new QueryRegistry()));
        mod.IsActive.Should().BeTrue();

        reg.Disable("a");

        mod.IsActive.Should().BeFalse();
        reg.IsEnabled("a").Should().BeFalse();
        reg.GetState("a").Should().Be(ModuleState.Disabled);
    }

    [Fact]
    public void Disable_Emits_ModuleDisabledEvent()
    {
        var bus = new EventBus();
        var disabled = new List<string>();
        bus.Subscribe<ModuleDisabledEvent>(e => disabled.Add(e.ModuleId));

        var reg = new ModuleRegistry(bus);
        reg.Register(new TestModule("a"));
        reg.ResolveAndEnable(CreateContext(bus, new QueryRegistry()));

        reg.Disable("a");

        disabled.Should().Equal("a");
    }

    [Fact]
    public void ReEnable_Restores_Module()
    {
        var bus = new EventBus();
        var reg = new ModuleRegistry(bus);
        var mod = new TestModule("a");
        reg.Register(mod);
        reg.ResolveAndEnable(CreateContext(bus, new QueryRegistry()));
        reg.Disable("a");

        reg.Enable("a");

        mod.IsActive.Should().BeTrue();
        reg.IsEnabled("a").Should().BeTrue();
        reg.GetState("a").Should().Be(ModuleState.Enabled);
    }

    [Fact]
    public void Disabled_Module_Query_Falls_Back_To_NoOp()
    {
        var bus = new EventBus();
        var queries = new QueryRegistry();
        queries.RegisterFallback<ITestQuery>(new TestQueryFallback());

        var reg = new ModuleRegistry(bus);
        var mod = new TestModule("a", sharedRegistry: queries, value: 55.0);
        reg.Register(mod);
        reg.ResolveAndEnable(CreateContext(bus, queries));

        // Enabled: real impl returns 55
        queries.Get<ITestQuery>().GetValue().Should().Be(55.0);

        // Disabled: module removes its registration, fallback (21.0) takes over (RULE-009)
        reg.Disable("a");
        queries.Get<ITestQuery>().GetValue().Should().Be(21.0);
    }
}
