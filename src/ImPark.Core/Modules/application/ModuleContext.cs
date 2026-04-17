using ImPark.Shared.Defs;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using ImPark.Shared.Modules;
using ImPark.Shared.Queries;

namespace ImPark.Core.Modules;

public sealed class ModuleContext : IModuleContext
{
    public ModuleContext(
        IEventBus bus,
        IDefDatabase defs,
        World world,
        ITilemapQuery tilemap,
        ITimeQuery time,
        IQueryRegistry queries)
    {
        Bus = bus ?? throw new ArgumentNullException(nameof(bus));
        Defs = defs ?? throw new ArgumentNullException(nameof(defs));
        World = world ?? throw new ArgumentNullException(nameof(world));
        Tilemap = tilemap ?? throw new ArgumentNullException(nameof(tilemap));
        Time = time ?? throw new ArgumentNullException(nameof(time));
        Queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public IEventBus Bus { get; }
    public IDefDatabase Defs { get; }
    public World World { get; }
    public ITilemapQuery Tilemap { get; }
    public ITimeQuery Time { get; }
    public IQueryRegistry Queries { get; }
}
