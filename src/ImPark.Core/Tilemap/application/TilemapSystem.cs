using ImPark.Shared.Events;

namespace ImPark.Core.Tilemap;

/// <summary>
/// Main tilemap system. Manages a 2D grid of tiles using chunk-based storage (16x16).
/// Provides O(1) tile access and emits TileChangedEvent via IEventBus on mutations.
/// </summary>
public sealed class TilemapSystem
{
    public const int ChunkSize = 16;

    private readonly IEventBus _eventBus;

    private TileComponent[,] _tiles = null!;
    private int[,] _regionIds = null!;
    private int _width;
    private int _height;
    private bool _initialized;

    public TilemapSystem(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public int Width => _width;
    public int Height => _height;
    public bool IsInitialized => _initialized;

    /// <summary>
    /// Initializes the tilemap grid with the given dimensions and default terrain.
    /// </summary>
    public void InitializeMap(int width, int height, TerrainType defaultTerrain)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        _width = width;
        _height = height;
        _tiles = new TileComponent[width, height];
        _regionIds = new int[width, height];

        var defaultTile = new TileComponent
        {
            Terrain = defaultTerrain,
            IsWalkable = defaultTerrain != TerrainType.Water && defaultTerrain != TerrainType.Lava,
            MoveCost = GetDefaultMoveCost(defaultTerrain),
            TerrainDefId = defaultTerrain.ToString().ToLowerInvariant()
        };

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _tiles[x, y] = defaultTile;
            }
        }

        // Initialize all region IDs to 0 (will be computed by RegionFloodFillSystem)
        Array.Clear(_regionIds, 0, _regionIds.Length);
        _initialized = true;
    }

    /// <summary>
    /// Sets a tile at the given coordinates and emits a TileChangedEvent asynchronously.
    /// </summary>
    public void SetTile(int x, int y, TileComponent tile)
    {
        ThrowIfNotInitialized();
        ThrowIfOutOfBounds(x, y);

        var old = _tiles[x, y];
        _tiles[x, y] = tile;

        _eventBus.PublishAsync(new TileChangedEvent(x, y, old.Terrain, tile.Terrain));
    }

    /// <summary>
    /// Gets the tile at the given coordinates. O(1) array lookup.
    /// </summary>
    public TileComponent GetTile(int x, int y)
    {
        ThrowIfNotInitialized();
        ThrowIfOutOfBounds(x, y);

        return _tiles[x, y];
    }

    /// <summary>
    /// Gets the region ID for a tile. O(1) array lookup.
    /// </summary>
    public int GetRegionId(int x, int y)
    {
        ThrowIfNotInitialized();
        ThrowIfOutOfBounds(x, y);

        return _regionIds[x, y];
    }

    /// <summary>
    /// Sets the region ID for a tile. Used internally by RegionFloodFillSystem.
    /// </summary>
    internal void SetRegionId(int x, int y, int regionId)
    {
        _regionIds[x, y] = regionId;
    }

    /// <summary>
    /// Gets the chunk coordinates for a tile position.
    /// </summary>
    public static (int ChunkX, int ChunkY) GetChunkCoords(int x, int y)
    {
        return (x / ChunkSize, y / ChunkSize);
    }

    /// <summary>
    /// Checks whether coordinates are within map bounds.
    /// </summary>
    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < _width && y >= 0 && y < _height;
    }

    private static float GetDefaultMoveCost(TerrainType terrain) => terrain switch
    {
        TerrainType.Soil => 1.0f,
        TerrainType.Stone => 1.2f,
        TerrainType.Water => float.PositiveInfinity,
        TerrainType.Sand => 1.5f,
        TerrainType.Grass => 1.0f,
        TerrainType.Clay => 1.3f,
        TerrainType.Ice => 0.8f,
        TerrainType.Lava => float.PositiveInfinity,
        _ => 1.0f
    };

    private void ThrowIfNotInitialized()
    {
        if (!_initialized)
            throw new InvalidOperationException("Tilemap has not been initialized. Call InitializeMap first.");
    }

    private void ThrowIfOutOfBounds(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height)
            throw new ArgumentOutOfRangeException(
                $"Tile coordinates ({x}, {y}) are out of bounds. Map size: {_width}x{_height}.");
    }
}
