using System.Collections.Generic;
using ImPark.Shared.ECS;

namespace ImPark.Pawn.Domain.Components;

// Deviation from RULE-001 (pure data struct with only value types):
// Skills holds a Dictionary reference because skill records mutate infrequently
// and Dictionary<string, SkillRecord> is idiomatic for a per-pawn skill registry.
public struct SkillsComponent : IComponent
{
    public Dictionary<string, SkillRecord> Skills;
}
