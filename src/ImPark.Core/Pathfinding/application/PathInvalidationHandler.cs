using ImPark.Core.Pathfinding.Domain;
using ImPark.Core.Tilemap;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using ImPark.Shared.Geometry;

namespace ImPark.Core.Pathfinding.Application;

// Listens for tile.changed events; evicts cached paths containing the
// changed tile and emits path.invalidated for any in-flight request that
// references the tile (start, goal, or multi-goal list).
public sealed class PathInvalidationHandler : ISystem, IDisposable
{
    private readonly PathCache _cache;
    private readonly PathRequestService _requestService;
    private readonly IEventBus _eventBus;
    private readonly IDisposable _subscription;

    public PathInvalidationHandler(
        PathCache cache,
        PathRequestService requestService,
        IEventBus eventBus)
    {
        _cache = cache;
        _requestService = requestService;
        _eventBus = eventBus;
        _subscription = _eventBus.Subscribe<TileChangedEvent>(OnTileChanged);
    }

    public int Order => 100;

    public void Update(World world, long currentTick) { }

    private void OnTileChanged(TileChangedEvent evt)
    {
        var tile = new Point(evt.X, evt.Y);
        _cache.EvictContaining(tile);

        foreach (var kvp in _requestService.InFlight)
        {
            var req = kvp.Value;
            if (ContainsTile(req, tile))
            {
                _eventBus.PublishAsync(new PathInvalidatedEvent(
                    req.Handle.Id, req.EntityId, tile));
            }
        }
    }

    private static bool ContainsTile(PathRequestService.QueuedRequest req, Point tile)
    {
        if (req.Start == tile) return true;
        var goals = req.Goals;
        for (int i = 0; i < goals.Length; i++)
            if (goals[i] == tile) return true;
        return false;
    }

    public void Dispose() => _subscription.Dispose();
}
