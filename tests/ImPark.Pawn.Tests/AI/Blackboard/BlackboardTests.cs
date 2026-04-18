using FluentAssertions;
using ImPark.Pawn.Domain.AI.Blackboard;
using ImPark.Shared.Geometry;
using Xunit;
using BB = ImPark.Pawn.Domain.AI.Blackboard.Blackboard;

namespace ImPark.Pawn.Tests.AI.Blackboard;

public sealed class BlackboardTests
{
    [Fact]
    public void Point_RoundTrip()
    {
        var bb = new BB(1);
        var p = new Point(3, 4);
        bb.Set("target", p);
        bb.Get<Point>("target").Should().Be(p);
    }

    [Fact]
    public void EntityId_RoundTrip()
    {
        var bb = new BB(1);
        bb.Set("self", 42L);
        bb.Get<long>("self").Should().Be(42L);
    }

    [Fact]
    public void Float_RoundTrip()
    {
        var bb = new BB(1);
        bb.Set("speed", 2.5f);
        bb.Get<float>("speed").Should().Be(2.5f);
    }

    [Fact]
    public void Int_RoundTrip()
    {
        var bb = new BB(1);
        bb.Set("count", 7);
        bb.Get<int>("count").Should().Be(7);
    }

    [Fact]
    public void Bool_RoundTrip()
    {
        var bb = new BB(1);
        bb.Set("flag", true);
        bb.Get<bool>("flag").Should().BeTrue();
    }

    [Fact]
    public void String_RoundTrip()
    {
        var bb = new BB(1);
        bb.Set("name", "hello");
        bb.Get<string>("name").Should().Be("hello");
    }

    [Fact]
    public void PathRequestToken_RoundTrip()
    {
        var bb = new BB(1);
        bb.Set("handle", (ulong)12345UL);
        bb.Get<ulong>("handle").Should().Be(12345UL);
    }

    [Fact]
    public void TryGet_Missing_Returns_False()
    {
        var bb = new BB(1);
        bb.TryGet<int>("missing", out var v).Should().BeFalse();
    }

    [Fact]
    public void TryGet_Present_Returns_True()
    {
        var bb = new BB(1);
        bb.Set("x", 9);
        bb.TryGet<int>("x", out var v).Should().BeTrue();
        v.Should().Be(9);
    }

    private sealed class UnsupportedType { }

    [Fact]
    public void Whitelist_Rejects_Arbitrary_Object()
    {
        var bb = new BB(1);
        var act = () => bb.Set("bad", new UnsupportedType());
        act.Should().Throw<ArgumentException>();
    }
}
