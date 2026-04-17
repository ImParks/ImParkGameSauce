using ImPark.Shared.ECS;
using ImPark.Shared.Queries;

namespace ImPark.Core.Time;

public sealed class TimeQuery : ITimeQuery
{
    private readonly World world;
    private readonly EntityId timeEntity;

    public TimeQuery(World world, EntityId timeEntity)
    {
        this.world = world;
        this.timeEntity = timeEntity;
    }

    public long GetCurrentTick()
    {
        ref var state = ref world.GetComponent<TimeState>(timeEntity);
        return state.CurrentTick;
    }

    public GameSpeed GetSpeed()
    {
        ref var state = ref world.GetComponent<TimeState>(timeEntity);
        return state.Speed;
    }

    public bool IsPaused()
    {
        ref var state = ref world.GetComponent<TimeState>(timeEntity);
        return state.IsPaused || state.Speed == GameSpeed.Paused;
    }
}
