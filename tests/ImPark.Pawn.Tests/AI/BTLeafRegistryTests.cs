using FluentAssertions;
using ImPark.Pawn.Application.AI.Leaves;
using ImPark.Pawn.Domain.AI.BT;
using Xunit;
using BB = ImPark.Pawn.Domain.AI.Blackboard.Blackboard;

namespace ImPark.Pawn.Tests.AI;

public sealed class BTLeafRegistryTests
{
    private sealed class StubNode : IBTNode
    {
        public BTStatus Tick(BB bb, float dt) => BTStatus.Success;
    }

    [Fact]
    public void Resolve_Registered_BuiltIn_Selector()
    {
        var reg = new BTLeafRegistry();
        var spec = new BTNodeSpec("selector", new(), Array.Empty<BTNodeSpec>());
        var node = reg.Resolve(spec);
        node.Should().BeOfType<Selector>();
    }

    [Fact]
    public void Resolve_Registered_BuiltIn_Sequence()
    {
        var reg = new BTLeafRegistry();
        var spec = new BTNodeSpec("sequence", new(), Array.Empty<BTNodeSpec>());
        reg.Resolve(spec).Should().BeOfType<Sequence>();
    }

    [Fact]
    public void Resolve_Registered_BuiltIn_Parallel_Policy()
    {
        var reg = new BTLeafRegistry();
        var parms = new Dictionary<string, object> { ["policy"] = "RequireOne" };
        var spec = new BTNodeSpec("parallel", parms, Array.Empty<BTNodeSpec>());
        reg.Resolve(spec).Should().BeOfType<Parallel>();
    }

    [Fact]
    public void Resolve_Unknown_Throws()
    {
        var reg = new BTLeafRegistry();
        var spec = new BTNodeSpec("unknown-leaf", new(), Array.Empty<BTNodeSpec>());
        var act = () => reg.Resolve(spec);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Register_And_Resolve_Custom_Mod_Leaf()
    {
        var reg = new BTLeafRegistry();
        reg.Register("TestMod.Custom", _ => new StubNode());
        var spec = new BTNodeSpec("TestMod.Custom", new(), Array.Empty<BTNodeSpec>());
        reg.Resolve(spec).Should().BeOfType<StubNode>();
    }

    [Fact]
    public void Register_Then_Resolve_RoundTrip_Preserves_Factory_Identity()
    {
        var reg = new BTLeafRegistry();
        var expected = new StubNode();
        reg.Register("identity-leaf", _ => expected);
        var spec = new BTNodeSpec("identity-leaf", new(), Array.Empty<BTNodeSpec>());
        var resolved = reg.Resolve(spec);
        ReferenceEquals(resolved, expected).Should().BeTrue();
    }
}
