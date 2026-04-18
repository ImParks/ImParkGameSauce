using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using ImPark.Pawn.Domain.Components;
using ImPark.Shared.ECS;
using Xunit;

namespace ImPark.Pawn.Tests.Components;

public sealed class PawnComponentsTests
{
    public static TheoryData<Type> PureDataComponents => new()
    {
        typeof(PawnTag),
        typeof(PawnPositionComponent),
        typeof(PawnMoveSpeedComponent),
        typeof(WorkCapabilityComponent),
        typeof(PawnBusyComponent),
    };

    [Theory]
    [MemberData(nameof(PureDataComponents))]
    public void Component_Is_Struct(Type t)
    {
        t.IsValueType.Should().BeTrue($"{t.Name} must be a struct");
    }

    [Theory]
    [MemberData(nameof(PureDataComponents))]
    public void Component_Implements_IComponent(Type t)
    {
        typeof(IComponent).IsAssignableFrom(t)
            .Should().BeTrue($"{t.Name} must implement IComponent");
    }

    [Theory]
    [MemberData(nameof(PureDataComponents))]
    public void Component_Has_No_Declared_Methods(Type t)
    {
        // Structs inherit Equals/GetHashCode/ToString from object; we check for
        // methods declared directly on the type, excluding compiler-generated helpers
        // and property accessors.
        var methods = t.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName)
            .Where(m => !m.Name.StartsWith("<"))
            .ToArray();

        methods.Should().BeEmpty(
            $"{t.Name} must be a pure data struct; found methods: " +
            string.Join(", ", methods.Select(m => m.Name)));
    }

    [Fact]
    public void SkillsComponent_Implements_IComponent()
    {
        typeof(IComponent).IsAssignableFrom(typeof(SkillsComponent))
            .Should().BeTrue();
        typeof(SkillsComponent).IsValueType.Should().BeTrue();
    }

    [Fact]
    public void SkillRecord_Is_Struct()
    {
        typeof(SkillRecord).IsValueType.Should().BeTrue();
    }
}
