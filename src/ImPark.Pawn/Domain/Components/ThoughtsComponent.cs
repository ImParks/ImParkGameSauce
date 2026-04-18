using System.Collections.Generic;
using ImPark.Shared.ECS;

namespace ImPark.Pawn.Domain.Components;

// NOTE: Thoughts is a class reference inside a value-type component. This is
// an accepted deviation from RULE-001 (pure-value components) because the
// active-thought list has variable length per pawn and we want to avoid
// per-tick reallocation. Mutations go through the list in place; the
// component itself is still looked up by struct semantics via World.Query.
public struct ThoughtsComponent : IComponent
{
    public List<ActiveThought> Thoughts;
}
