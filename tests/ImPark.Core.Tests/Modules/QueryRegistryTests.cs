using FluentAssertions;
using ImPark.Core.Modules;
using ImPark.Shared.Modules;
using Xunit;

namespace ImPark.Core.Tests.Modules;

public interface ITestQuery
{
    double GetValue();
}

internal sealed class RealTestQuery : ITestQuery
{
    public double Value { get; init; } = 42.0;
    public double GetValue() => Value;
}

internal sealed class TestQueryFallback : IModuleFallback<ITestQuery>
{
    public ITestQuery CreateFallback() => new RealTestQuery { Value = 21.0 };
}

public class QueryRegistryTests
{
    [Fact]
    public void Register_Then_Get_Returns_Registered_Impl()
    {
        var reg = new QueryRegistry();
        var impl = new RealTestQuery { Value = 42.0 };

        reg.Register<ITestQuery>(impl);

        reg.Get<ITestQuery>().Should().BeSameAs(impl);
        reg.Get<ITestQuery>().GetValue().Should().Be(42.0);
    }

    [Fact]
    public void Register_Strict_Throws_On_Duplicate()
    {
        var reg = new QueryRegistry();
        reg.Register<ITestQuery>(new RealTestQuery());

        var act = () => reg.Register<ITestQuery>(new RealTestQuery(), RegistrationPolicy.Strict);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Register_Replace_Overwrites_Existing()
    {
        var reg = new QueryRegistry();
        reg.Register<ITestQuery>(new RealTestQuery { Value = 10.0 });

        var replacement = new RealTestQuery { Value = 99.0 };
        reg.Register<ITestQuery>(replacement, RegistrationPolicy.Replace);

        reg.Get<ITestQuery>().Should().BeSameAs(replacement);
    }

    [Fact]
    public void TryGet_Returns_Null_When_Not_Registered()
    {
        var reg = new QueryRegistry();

        reg.TryGet<ITestQuery>().Should().BeNull();
    }

    [Fact]
    public void TryGet_Returns_Impl_When_Registered()
    {
        var reg = new QueryRegistry();
        var impl = new RealTestQuery();
        reg.Register<ITestQuery>(impl);

        reg.TryGet<ITestQuery>().Should().BeSameAs(impl);
    }

    [Fact]
    public void Get_Throws_When_No_Impl_And_No_Fallback()
    {
        var reg = new QueryRegistry();

        var act = () => reg.Get<ITestQuery>();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Get_Uses_Fallback_When_Impl_Not_Registered()
    {
        var reg = new QueryRegistry();
        reg.RegisterFallback<ITestQuery>(new TestQueryFallback());

        var result = reg.Get<ITestQuery>();

        result.GetValue().Should().Be(21.0);
    }

    [Fact]
    public void Get_Prefers_Impl_Over_Fallback()
    {
        var reg = new QueryRegistry();
        reg.RegisterFallback<ITestQuery>(new TestQueryFallback());
        reg.Register<ITestQuery>(new RealTestQuery { Value = 42.0 });

        reg.Get<ITestQuery>().GetValue().Should().Be(42.0);
    }
}
