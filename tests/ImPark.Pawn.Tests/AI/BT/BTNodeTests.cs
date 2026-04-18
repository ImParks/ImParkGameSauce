using FluentAssertions;
using ImPark.Pawn.Domain.AI.BT;
using Xunit;
using BB = ImPark.Pawn.Domain.AI.Blackboard.Blackboard;

namespace ImPark.Pawn.Tests.AI.BT;

public sealed class BTNodeTests
{
    private static BB NewBB() => new(pawnId: 1);

    private sealed class FakeNode : IBTNode
    {
        public BTStatus Result;
        public int TickCount;
        public BTStatus Tick(BB bb, float dt)
        {
            TickCount++;
            return Result;
        }
    }

    [Fact]
    public void Selector_ShortCircuits_On_Success()
    {
        var a = new FakeNode { Result = BTStatus.Failure };
        var b = new FakeNode { Result = BTStatus.Success };
        var c = new FakeNode { Result = BTStatus.Failure };

        var sel = new Selector(a, b, c);
        var result = sel.Tick(NewBB(), 0.016f);

        result.Should().Be(BTStatus.Success);
        a.TickCount.Should().Be(1);
        b.TickCount.Should().Be(1);
        c.TickCount.Should().Be(0);
    }

    [Fact]
    public void Selector_All_Failure_Returns_Failure()
    {
        var a = new FakeNode { Result = BTStatus.Failure };
        var b = new FakeNode { Result = BTStatus.Failure };

        var result = new Selector(a, b).Tick(NewBB(), 0.016f);
        result.Should().Be(BTStatus.Failure);
    }

    [Fact]
    public void Selector_Running_ShortCircuits()
    {
        var a = new FakeNode { Result = BTStatus.Failure };
        var b = new FakeNode { Result = BTStatus.Running };
        var c = new FakeNode { Result = BTStatus.Success };

        var result = new Selector(a, b, c).Tick(NewBB(), 0.016f);
        result.Should().Be(BTStatus.Running);
        c.TickCount.Should().Be(0);
    }

    [Fact]
    public void Sequence_ShortCircuits_On_Failure()
    {
        var a = new FakeNode { Result = BTStatus.Success };
        var b = new FakeNode { Result = BTStatus.Failure };
        var c = new FakeNode { Result = BTStatus.Success };

        var result = new Sequence(a, b, c).Tick(NewBB(), 0.016f);
        result.Should().Be(BTStatus.Failure);
        c.TickCount.Should().Be(0);
    }

    [Fact]
    public void Sequence_All_Success_Returns_Success()
    {
        var a = new FakeNode { Result = BTStatus.Success };
        var b = new FakeNode { Result = BTStatus.Success };

        var result = new Sequence(a, b).Tick(NewBB(), 0.016f);
        result.Should().Be(BTStatus.Success);
    }

    [Fact]
    public void Inverter_Swaps_Success_And_Failure()
    {
        new Inverter(new FakeNode { Result = BTStatus.Success })
            .Tick(NewBB(), 0.016f).Should().Be(BTStatus.Failure);
        new Inverter(new FakeNode { Result = BTStatus.Failure })
            .Tick(NewBB(), 0.016f).Should().Be(BTStatus.Success);
    }

    [Fact]
    public void Inverter_Passes_Running_Through()
    {
        new Inverter(new FakeNode { Result = BTStatus.Running })
            .Tick(NewBB(), 0.016f).Should().Be(BTStatus.Running);
    }

    [Fact]
    public void Parallel_RequireAll_All_Success()
    {
        var a = new FakeNode { Result = BTStatus.Success };
        var b = new FakeNode { Result = BTStatus.Success };
        var par = new Parallel(ParallelPolicy.RequireAll, a, b);
        par.Tick(NewBB(), 0.016f).Should().Be(BTStatus.Success);
    }

    [Fact]
    public void Parallel_RequireAll_Any_Failure_Returns_Failure()
    {
        var a = new FakeNode { Result = BTStatus.Success };
        var b = new FakeNode { Result = BTStatus.Failure };
        var par = new Parallel(ParallelPolicy.RequireAll, a, b);
        par.Tick(NewBB(), 0.016f).Should().Be(BTStatus.Failure);
    }

    [Fact]
    public void Parallel_RequireOne_First_Success_Returns_Success()
    {
        var a = new FakeNode { Result = BTStatus.Success };
        var b = new FakeNode { Result = BTStatus.Running };
        var par = new Parallel(ParallelPolicy.RequireOne, a, b);
        par.Tick(NewBB(), 0.016f).Should().Be(BTStatus.Success);
    }

    [Fact]
    public void Parallel_RequireOne_All_Failure_Returns_Failure()
    {
        var a = new FakeNode { Result = BTStatus.Failure };
        var b = new FakeNode { Result = BTStatus.Failure };
        var par = new Parallel(ParallelPolicy.RequireOne, a, b);
        par.Tick(NewBB(), 0.016f).Should().Be(BTStatus.Failure);
    }

    [Fact]
    public void Parallel_Mixed_Running_Returns_Running()
    {
        var a = new FakeNode { Result = BTStatus.Running };
        var b = new FakeNode { Result = BTStatus.Running };
        new Parallel(ParallelPolicy.RequireAll, a, b)
            .Tick(NewBB(), 0.016f).Should().Be(BTStatus.Running);
        new Parallel(ParallelPolicy.RequireOne, a, b)
            .Tick(NewBB(), 0.016f).Should().Be(BTStatus.Running);
    }
}
