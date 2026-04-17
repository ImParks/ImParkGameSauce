using ImPark.Shared.ECS;
using ImPark.Shared.Queries;

namespace ImPark.Core.Time;

public struct TimeState : IComponent
{
    public long CurrentTick;
    public GameSpeed Speed;
    public bool IsPaused;
}
