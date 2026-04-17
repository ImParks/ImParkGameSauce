using FluentAssertions;
using ImPark.Shared.Defs;
using Xunit;

namespace ImPark.Shared.Tests.Defs;

public class DefDatabaseTests : IDisposable
{
    public DefDatabaseTests()
    {
        DefDatabase.Instance.Clear();
    }

    public void Dispose()
    {
        DefDatabase.Instance.Clear();
    }

    [Fact]
    public void Register_And_Get_RoundTrip()
    {
        var def = new ThingDef { DefId = "wood", Label = "Wood", Description = "A piece of wood" };

        DefDatabase.Instance.Register(def);

        var retrieved = DefDatabase.Instance.Get<ThingDef>("wood");
        retrieved.Should().NotBeNull();
        retrieved!.DefId.Should().Be("wood");
        retrieved.Label.Should().Be("Wood");
        retrieved.Description.Should().Be("A piece of wood");
    }

    [Fact]
    public void Get_ReturnsNull_WhenDefIdNotFound()
    {
        var def = new ThingDef { DefId = "stone" };
        DefDatabase.Instance.Register(def);

        var result = DefDatabase.Instance.Get<ThingDef>("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public void Get_ReturnsNull_WhenTypeMismatch()
    {
        var def = new ThingDef { DefId = "wood" };
        DefDatabase.Instance.Register(def);

        var result = DefDatabase.Instance.Get<HediffDef>("wood");

        result.Should().BeNull();
    }

    [Fact]
    public void AllOf_ReturnsAllDefsOfType()
    {
        DefDatabase.Instance.Register(new ThingDef { DefId = "wood" });
        DefDatabase.Instance.Register(new ThingDef { DefId = "stone" });
        DefDatabase.Instance.Register(new HediffDef { DefId = "burn" });

        var things = DefDatabase.Instance.AllOf<ThingDef>();

        things.Should().HaveCount(2);
        things.Select(d => d.DefId).Should().BeEquivalentTo("wood", "stone");
    }

    [Fact]
    public void AllOf_ReturnsEmpty_WhenNoDefsOfType()
    {
        DefDatabase.Instance.Register(new ThingDef { DefId = "wood" });

        var hediffs = DefDatabase.Instance.AllOf<HediffDef>();

        hediffs.Should().BeEmpty();
    }

    [Fact]
    public void Sealed_Database_Rejects_Register()
    {
        DefDatabase.Instance.Register(new ThingDef { DefId = "wood" });
        DefDatabase.Instance.Seal();

        var act = () => DefDatabase.Instance.Register(new ThingDef { DefId = "stone" });

        act.Should().Throw<InvalidOperationException>();
        DefDatabase.Instance.IsSealed.Should().BeTrue();
    }

    [Fact]
    public void Sealed_Database_Still_Allows_Reads()
    {
        DefDatabase.Instance.Register(new ThingDef { DefId = "wood", Label = "Wood" });
        DefDatabase.Instance.Seal();

        var def = DefDatabase.Instance.Get<ThingDef>("wood");

        def.Should().NotBeNull();
        def!.Label.Should().Be("Wood");
    }

    [Fact]
    public void Register_ThrowsOnEmpty_DefId()
    {
        var def = new ThingDef { DefId = "" };

        var act = () => DefDatabase.Instance.Register(def);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Clear_Unseals_And_RemovesAllData()
    {
        DefDatabase.Instance.Register(new ThingDef { DefId = "wood" });
        DefDatabase.Instance.Seal();

        DefDatabase.Instance.Clear();

        DefDatabase.Instance.IsSealed.Should().BeFalse();
        DefDatabase.Instance.Get<ThingDef>("wood").Should().BeNull();
        DefDatabase.Instance.AllOf<ThingDef>().Should().BeEmpty();
    }
}

public class NullDefDatabaseTests
{
    [Fact]
    public void Get_ReturnsNull()
    {
        var db = new NullDefDatabase();

        db.Get<ThingDef>("anything").Should().BeNull();
    }

    [Fact]
    public void AllOf_ReturnsEmpty()
    {
        var db = new NullDefDatabase();

        db.AllOf<ThingDef>().Should().BeEmpty();
    }

    [Fact]
    public void IsSealed_ReturnsTrue()
    {
        var db = new NullDefDatabase();

        db.IsSealed.Should().BeTrue();
    }
}
