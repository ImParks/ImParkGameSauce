using ImPark.Shared.Defs;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using ImPark.Shared.Queries;

namespace ImPark.Shared.Modules;

public interface IModuleContext
{
    IEventBus Bus { get; }
    IDefDatabase Defs { get; }
    World World { get; }
    ITilemapQuery Tilemap { get; }
    ITimeQuery Time { get; }
    IQueryRegistry Queries { get; }
}
