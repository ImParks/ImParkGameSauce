using ImPark.Shared.ECS;
using ImPark.Shared.Events;

namespace ImPark.Core.Tilemap;

/// <summary>
/// ISystem that recomputes walkable region IDs via 4-connected flood fill.
/// Subscribes to TileChangedEvent and marks affected areas dirty.
/// On Update, runs flood fill only for dirty regions.
/// </summary>
public sealed class RegionFloodFillSystem : ISystem, IDisposable
{
    private static readonly (int Dx, int Dy)[] Directions =
    {
        (0, 1), (0, -1), (1, 0), (-1, 0)
    };

    private readonly TilemapSystem _tilemap;
    private readonly IDisposable _subscription;
    private bool _fullRebuildRequired;
    private readonly HashSet<(int X, int Y)> _dirtyTiles = new();
    private readonly Queue<(int X, int Y)> _fillQueue = new();

    public int Order => 100;

    public RegionFloodFillSystem(TilemapSystem tilemap, IEventBus eventBus)
    {
        _tilemap = tilemap;
        _subscription = eventBus.Subscribe<TileChangedEvent>(OnTileChanged);
    }

    private void OnTileChanged(TileChangedEvent evt)
    {
        _dirtyTiles.Add((evt.X, evt.Y));

        // Also mark 4-connected neighbors as dirty so region boundaries update
        foreach (var (dx, dy) in Directions)
        {
            int nx = evt.X + dx;
            int ny = evt.Y + dy;
            if (_tilemap.InBounds(nx, ny))
                _dirtyTiles.Add((nx, ny));
        }
    }

    /// <summary>
    /// Forces a full region rebuild on next Update. Used after InitializeMap.
    /// </summary>
    public void MarkFullRebuild()
    {
        _fullRebuildRequired = true;
    }

    public void Update(World world, long currentTick)
    {
        if (!_tilemap.IsInitialized)
            return;

        if (_fullRebuildRequired)
        {
            RebuildAllRegions();
            _fullRebuildRequired = false;
            _dirtyTiles.Clear();
            return;
        }

        if (_dirtyTiles.Count == 0)
            return;

        // For dirty tiles we do a full rebuild since region IDs can cascade globally.
        // A partial rebuild would be incorrect when splitting/merging regions.
        RebuildAllRegions();
        _dirtyTiles.Clear();
    }

    /// <summary>
    /// Rebuilds all region IDs from scratch using 4-connected flood fill on walkable tiles.
    /// Non-walkable tiles get RegionId = 0.
    /// </summary>
    private void RebuildAllRegions()
    {
        int width = _tilemap.Width;
        int height = _tilemap.Height;
        int currentRegion = 0;

        // Reset all region IDs
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _tilemap.SetRegionId(x, y, 0);
            }
        }

        // Flood fill each unvisited walkable tile
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                var tile = _tilemap.GetTile(x, y);
                if (!tile.IsWalkable)
                    continue;

                if (_tilemap.GetRegionId(x, y) != 0)
                    continue;

                currentRegion++;
                FloodFill(x, y, currentRegion, width, height);
            }
        }
    }

    private void FloodFill(int startX, int startY, int regionId, int width, int height)
    {
        _fillQueue.Clear();
        _fillQueue.Enqueue((startX, startY));
        _tilemap.SetRegionId(startX, startY, regionId);

        while (_fillQueue.Count > 0)
        {
            var (cx, cy) = _fillQueue.Dequeue();

            foreach (var (dx, dy) in Directions)
            {
                int nx = cx + dx;
                int ny = cy + dy;

                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    continue;

                if (_tilemap.GetRegionId(nx, ny) != 0)
                    continue;

                var neighborTile = _tilemap.GetTile(nx, ny);
                if (!neighborTile.IsWalkable)
                    continue;

                _tilemap.SetRegionId(nx, ny, regionId);
                _fillQueue.Enqueue((nx, ny));
            }
        }
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}
