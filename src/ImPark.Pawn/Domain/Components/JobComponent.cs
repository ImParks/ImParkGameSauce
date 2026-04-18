using ImPark.Pawn.Domain.AI.Jobs;
using ImPark.Shared.ECS;

namespace ImPark.Pawn.Domain.Components;

// RULE-001 deviation: contains a reference field (Driver) so the component is
// not strictly pure data. The active job driver is transient runtime state and
// cannot be expressed as a value type without allocating per tick.
public struct JobComponent : IComponent
{
    public Job Current;
    public IJobDriver? Driver;
}
