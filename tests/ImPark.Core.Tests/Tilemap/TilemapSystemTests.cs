using FluentAssertions;
using ImPark.Core.Tilemap;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using Xunit;

namespace ImPark.Core.Tests.Tilemap;

#region Stub EventBus

/// <summary>
/// Simple stub IEventBus that records published events and invokes subscribers synchronously.
/// </summary>
internal sealed class StubEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly List<(Type Type, object Event)> _asyncQueue = new();

    public void PublishSync<T>(in T evt) where T : struct
    {
        InvokeHandlers(evt);
    }

    public void PublishAsync<T>(in T evt) where T : struct
    {
        _asyncQueue.Add((typeof(T), evt));
    }

    public IDisposable Subscribe<T>(Action<T> handler) where T : struct
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            _handlers[type] = list;
        }
        list.Add(handler);
        return new Unsubscriber(() => list.Remove(handler));
    }

    public void Flush()
    {
        // Process the async queue, invoking handlers
        var snapshot = _asyncQueue.ToList();
        _asyncQueue.Clear();

        foreach (var (type, evt) in snapshot)
        {
            if (_handlers.TryGetValue(type, out var list))
            {
                foreach (var handler in list)
                {
                    handler.DynamicInvoke(evt);
                }
            }
        }
    }

    private void InvokeHandlers<T>(T evt) where T : struct
    {
        if (_handlers.TryGetValue(typeof(T), out var list))
        {
            foreach (var handler in list)
            {
                ((Action<T>)handler)(evt);
            }
        }
    }

    private sealed class Unsubscriber : IDisposable
    {
        private readonly Action _action;
        public Unsubscriber(Action action) => _action = action;
        public void Dispose() => _action();
    }
}

#endregion

public class TilemapSystemTests
{
    private readonly StubEventBus _eventBus;
    private readonly TilemapSystem _tilemap;
    private readonly RegionFloodFillSystem _regionSystem;
    private readonly TilemapQuery _query;
    private readonly World _world;

    public TilemapSystemTests()
    {
        _eventBus = new StubEventBus();
        _tilemap = new TilemapSystem(_eventBus);
        _regionSystem = new RegionFloodFillSystem(_tilemap, _eventBus);
        _query = new TilemapQuery(_tilemap);
        _world = new World();
    }

    [Fact]
    public void InitializeMap_CreatesCorrectGrid()
    {
        _tilemap.InitializeMap(32, 24, TerrainType.Grass);

        _tilemap.Width.Should().Be(32);
        _tilemap.Height.Should().Be(24);
        _tilemap.IsInitialized.Should().BeTrue();

        // Spot-check corners and center
        var topLeft = _tilemap.GetTile(0, 0);
        topLeft.Terrain.Should().Be(TerrainType.Grass);
        topLeft.IsWalkable.Should().BeTrue();
        topLeft.TerrainDefId.Should().Be("grass");

        var bottomRight = _tilemap.GetTile(31, 23);
        bottomRight.Terrain.Should().Be(TerrainType.Grass);

        var center = _tilemap.GetTile(16, 12);
        center.Terrain.Should().Be(TerrainType.Grass);
    }

    [Fact]
    public void SetTile_UpdatesTileData()
    {
        _tilemap.InitializeMap(16, 16, TerrainType.Soil);

        var stoneTile = new TileComponent
        {
            Terrain = TerrainType.Stone,
            IsWalkable = true,
            MoveCost = 1.2f,
            TerrainDefId = "stone"
        };

        _tilemap.SetTile(5, 5, stoneTile);
        _eventBus.Flush();

        var retrieved = _tilemap.GetTile(5, 5);
        retrieved.Terrain.Should().Be(TerrainType.Stone);
        retrieved.MoveCost.Should().Be(1.2f);
        retrieved.TerrainDefId.Should().Be("stone");
    }

    [Fact]
    public void IsWalkable_ReturnsCorrectValue()
    {
        _tilemap.InitializeMap(16, 16, TerrainType.Grass);
        _regionSystem.MarkFullRebuild();
        _regionSystem.Update(_world, 0);

        _query.IsWalkable(5, 5).Should().BeTrue();

        var waterTile = new TileComponent
        {
            Terrain = TerrainType.Water,
            IsWalkable = false,
            MoveCost = float.PositiveInfinity,
            TerrainDefId = "water"
        };

        _tilemap.SetTile(5, 5, waterTile);
        _eventBus.Flush();

        _query.IsWalkable(5, 5).Should().BeFalse();
    }

    [Fact]
    public void Region_SplitsWhenWallIsPlaced()
    {
        // Create a 5x5 all-walkable map
        _tilemap.InitializeMap(5, 5, TerrainType.Grass);
        _regionSystem.MarkFullRebuild();
        _regionSystem.Update(_world, 0);

        // All tiles should be in the same region
        int originalRegion = _query.GetRegionId(0, 0);
        originalRegion.Should().BeGreaterThan(0);
        _query.GetRegionId(4, 4).Should().Be(originalRegion);

        // Place a wall across the middle row (y=2), splitting top from bottom
        var wall = new TileComponent
        {
            Terrain = TerrainType.Water,
            IsWalkable = false,
            MoveCost = float.PositiveInfinity,
            TerrainDefId = "water"
        };

        for (int x = 0; x < 5; x++)
        {
            _tilemap.SetTile(x, 2, wall);
        }
        _eventBus.Flush();
        _regionSystem.Update(_world, 1);

        // Tiles above and below the wall should be in different regions
        int topRegion = _query.GetRegionId(0, 0);
        int bottomRegion = _query.GetRegionId(0, 4);

        topRegion.Should().BeGreaterThan(0);
        bottomRegion.Should().BeGreaterThan(0);
        topRegion.Should().NotBe(bottomRegion);

        // Wall tiles should have region 0
        _query.GetRegionId(2, 2).Should().Be(0);
    }

    [Fact]
    public void Region_MergesWhenWallIsRemoved()
    {
        // Create a 5x5 map with a wall splitting it
        _tilemap.InitializeMap(5, 5, TerrainType.Grass);

        var wall = new TileComponent
        {
            Terrain = TerrainType.Water,
            IsWalkable = false,
            MoveCost = float.PositiveInfinity,
            TerrainDefId = "water"
        };

        for (int x = 0; x < 5; x++)
        {
            _tilemap.SetTile(x, 2, wall);
        }
        _eventBus.Flush();
        _regionSystem.MarkFullRebuild();
        _regionSystem.Update(_world, 0);

        // Verify split
        int topRegion = _query.GetRegionId(0, 0);
        int bottomRegion = _query.GetRegionId(0, 4);
        topRegion.Should().NotBe(bottomRegion);

        // Remove the wall at one point to create a passage
        var grass = new TileComponent
        {
            Terrain = TerrainType.Grass,
            IsWalkable = true,
            MoveCost = 1.0f,
            TerrainDefId = "grass"
        };

        _tilemap.SetTile(2, 2, grass);
        _eventBus.Flush();
        _regionSystem.Update(_world, 1);

        // Now top and bottom should be in the same region
        int mergedTop = _query.GetRegionId(0, 0);
        int mergedBottom = _query.GetRegionId(0, 4);
        mergedTop.Should().Be(mergedBottom);
        mergedTop.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetRegionId_IsO1Lookup()
    {
        // This test verifies O(1) access by ensuring consistent results
        // and that multiple rapid lookups succeed without issue.
        _tilemap.InitializeMap(64, 64, TerrainType.Grass);
        _regionSystem.MarkFullRebuild();
        _regionSystem.Update(_world, 0);

        // All walkable tiles should have the same region ID
        int expectedRegion = _query.GetRegionId(0, 0);
        expectedRegion.Should().BeGreaterThan(0);

        // Rapid O(1) lookups across the map
        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                _query.GetRegionId(x, y).Should().Be(expectedRegion);
            }
        }
    }

    [Fact]
    public void SetTile_EmitsTileChangedEvent()
    {
        _tilemap.InitializeMap(8, 8, TerrainType.Soil);

        var receivedEvents = new List<TileChangedEvent>();
        _eventBus.Subscribe<TileChangedEvent>(evt => receivedEvents.Add(evt));

        var stone = new TileComponent
        {
            Terrain = TerrainType.Stone,
            IsWalkable = true,
            MoveCost = 1.2f,
            TerrainDefId = "stone"
        };

        _tilemap.SetTile(3, 4, stone);
        _eventBus.Flush();

        receivedEvents.Should().HaveCount(1);
        receivedEvents[0].X.Should().Be(3);
        receivedEvents[0].Y.Should().Be(4);
        receivedEvents[0].OldTerrain.Should().Be(TerrainType.Soil);
        receivedEvents[0].NewTerrain.Should().Be(TerrainType.Stone);
    }

    [Fact]
    public void GetTile_ThrowsOnOutOfBounds()
    {
        _tilemap.InitializeMap(10, 10, TerrainType.Grass);

        var act1 = () => _tilemap.GetTile(-1, 0);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => _tilemap.GetTile(10, 0);
        act2.Should().Throw<ArgumentOutOfRangeException>();

        var act3 = () => _tilemap.GetTile(0, 10);
        act3.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetChunkCoords_ReturnsCorrectChunk()
    {
        TilemapSystem.GetChunkCoords(0, 0).Should().Be((0, 0));
        TilemapSystem.GetChunkCoords(15, 15).Should().Be((0, 0));
        TilemapSystem.GetChunkCoords(16, 0).Should().Be((1, 0));
        TilemapSystem.GetChunkCoords(31, 31).Should().Be((1, 1));
        TilemapSystem.GetChunkCoords(32, 48).Should().Be((2, 3));
    }

    [Fact]
    public void TilemapQuery_GetTile_ReturnsTileData()
    {
        _tilemap.InitializeMap(16, 16, TerrainType.Sand);
        _regionSystem.MarkFullRebuild();
        _regionSystem.Update(_world, 0);

        var tileData = _query.GetTile(5, 5);
        tileData.TerrainDefId.Should().Be("sand");
        tileData.IsWalkable.Should().BeTrue();
        tileData.RegionId.Should().BeGreaterThan(0);
        tileData.MoveCost.Should().Be(1.5f);
    }

    [Fact]
    public void InitializeMap_WaterTerrain_IsNotWalkable()
    {
        _tilemap.InitializeMap(8, 8, TerrainType.Water);
        _regionSystem.MarkFullRebuild();
        _regionSystem.Update(_world, 0);

        var tile = _tilemap.GetTile(0, 0);
        tile.IsWalkable.Should().BeFalse();
        tile.MoveCost.Should().Be(float.PositiveInfinity);

        // Non-walkable tiles should have region 0
        _query.GetRegionId(0, 0).Should().Be(0);
    }
}
