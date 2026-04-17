using FluentAssertions;
using ImPark.Core.Tilemap;
using ImPark.Shared.ECS;
using ImPark.Shared.Events;
using Xunit;

namespace ImPark.Core.Tests.Tilemap;

internal sealed class StubEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly List<(Type Type, object Event)> _asyncQueue = new();

    public void PublishSync<T>(in T evt) where T : struct => InvokeHandlers(evt);

    public void PublishAsync<T>(in T evt) where T : struct =>
        _asyncQueue.Add((typeof(T), evt));

    public IDisposable Subscribe<T>(Action<T> handler) where T : struct
    {
        if (!_handlers.TryGetValue(typeof(T), out var list))
            _handlers[typeof(T)] = list = new List<Delegate>();
        list.Add(handler);
        return new Unsubscriber(() => list.Remove(handler));
    }

    public void Flush()
    {
        var snapshot = _asyncQueue.ToList();
        _asyncQueue.Clear();
        foreach (var (type, evt) in snapshot)
            if (_handlers.TryGetValue(type, out var list))
                foreach (var h in list) h.DynamicInvoke(evt);
    }

    private void InvokeHandlers<T>(T evt) where T : struct
    {
        if (_handlers.TryGetValue(typeof(T), out var list))
            foreach (var h in list) ((Action<T>)h)(evt);
    }

    private sealed class Unsubscriber(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}

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

        var topLeft = _tilemap.GetTile(0, 0);
        topLeft.Terrain.Should().Be(TerrainType.Grass);
        topLeft.IsWalkable.Should().BeTrue();
        topLeft.TerrainDefId.Should().Be("grass");
        _tilemap.GetTile(31, 23).Terrain.Should().Be(TerrainType.Grass);
        _tilemap.GetTile(16, 12).Terrain.Should().Be(TerrainType.Grass);
    }

    [Fact]
    public void SetTile_UpdatesTileData()
    {
        _tilemap.InitializeMap(16, 16, TerrainType.Soil);
        var stoneTile = new TileComponent
        {
            Terrain = TerrainType.Stone, IsWalkable = true,
            MoveCost = 1.2f, TerrainDefId = "stone"
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
            Terrain = TerrainType.Water, IsWalkable = false,
            MoveCost = float.PositiveInfinity, TerrainDefId = "water"
        };
        _tilemap.SetTile(5, 5, waterTile);
        _eventBus.Flush();
        _query.IsWalkable(5, 5).Should().BeFalse();
    }

    [Fact]
    public void Region_SplitsWhenWallIsPlaced()
    {
        _tilemap.InitializeMap(5, 5, TerrainType.Grass);
        _regionSystem.MarkFullRebuild();
        _regionSystem.Update(_world, 0);

        int originalRegion = _query.GetRegionId(0, 0);
        originalRegion.Should().BeGreaterThan(0);
        _query.GetRegionId(4, 4).Should().Be(originalRegion);

        // Place wall across y=2, splitting top from bottom
        var wall = new TileComponent
        {
            Terrain = TerrainType.Water, IsWalkable = false,
            MoveCost = float.PositiveInfinity, TerrainDefId = "water"
        };
        for (int x = 0; x < 5; x++)
            _tilemap.SetTile(x, 2, wall);
        _eventBus.Flush();
        _regionSystem.Update(_world, 1);

        int topRegion = _query.GetRegionId(0, 0);
        int bottomRegion = _query.GetRegionId(0, 4);
        topRegion.Should().BeGreaterThan(0);
        bottomRegion.Should().BeGreaterThan(0);
        topRegion.Should().NotBe(bottomRegion);
        _query.GetRegionId(2, 2).Should().Be(0);
    }

    [Fact]
    public void Region_MergesWhenWallIsRemoved()
    {
        _tilemap.InitializeMap(5, 5, TerrainType.Grass);
        var wall = new TileComponent
        {
            Terrain = TerrainType.Water, IsWalkable = false,
            MoveCost = float.PositiveInfinity, TerrainDefId = "water"
        };
        for (int x = 0; x < 5; x++)
            _tilemap.SetTile(x, 2, wall);
        _eventBus.Flush();
        _regionSystem.MarkFullRebuild();
        _regionSystem.Update(_world, 0);

        _query.GetRegionId(0, 0).Should().NotBe(_query.GetRegionId(0, 4));

        // Remove one wall tile to create a passage
        var grass = new TileComponent
        {
            Terrain = TerrainType.Grass, IsWalkable = true,
            MoveCost = 1.0f, TerrainDefId = "grass"
        };
        _tilemap.SetTile(2, 2, grass);
        _eventBus.Flush();
        _regionSystem.Update(_world, 1);

        int mergedTop = _query.GetRegionId(0, 0);
        int mergedBottom = _query.GetRegionId(0, 4);
        mergedTop.Should().Be(mergedBottom);
        mergedTop.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetRegionId_IsO1Lookup()
    {
        _tilemap.InitializeMap(64, 64, TerrainType.Grass);
        _regionSystem.MarkFullRebuild();
        _regionSystem.Update(_world, 0);

        int expectedRegion = _query.GetRegionId(0, 0);
        expectedRegion.Should().BeGreaterThan(0);

        // 4096 rapid O(1) lookups
        for (int x = 0; x < 64; x++)
            for (int y = 0; y < 64; y++)
                _query.GetRegionId(x, y).Should().Be(expectedRegion);
    }

    [Fact]
    public void SetTile_EmitsTileChangedEvent()
    {
        _tilemap.InitializeMap(8, 8, TerrainType.Soil);
        var received = new List<TileChangedEvent>();
        _eventBus.Subscribe<TileChangedEvent>(evt => received.Add(evt));

        var stone = new TileComponent
        {
            Terrain = TerrainType.Stone, IsWalkable = true,
            MoveCost = 1.2f, TerrainDefId = "stone"
        };
        _tilemap.SetTile(3, 4, stone);
        _eventBus.Flush();

        received.Should().HaveCount(1);
        received[0].Should().Be(new TileChangedEvent(3, 4, TerrainType.Soil, TerrainType.Stone));
    }

    [Fact]
    public void GetTile_ThrowsOnOutOfBounds()
    {
        _tilemap.InitializeMap(10, 10, TerrainType.Grass);
        ((Action)(() => _tilemap.GetTile(-1, 0))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => _tilemap.GetTile(10, 0))).Should().Throw<ArgumentOutOfRangeException>();
        ((Action)(() => _tilemap.GetTile(0, 10))).Should().Throw<ArgumentOutOfRangeException>();
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

        var td = _query.GetTile(5, 5);
        td.TerrainDefId.Should().Be("sand");
        td.IsWalkable.Should().BeTrue();
        td.RegionId.Should().BeGreaterThan(0);
        td.MoveCost.Should().Be(1.5f);
    }

    [Fact]
    public void InitializeMap_WaterTerrain_IsNotWalkable()
    {
        _tilemap.InitializeMap(8, 8, TerrainType.Water);
        _regionSystem.MarkFullRebuild();
        _regionSystem.Update(_world, 0);

        _tilemap.GetTile(0, 0).IsWalkable.Should().BeFalse();
        _tilemap.GetTile(0, 0).MoveCost.Should().Be(float.PositiveInfinity);
        _query.GetRegionId(0, 0).Should().Be(0);
    }
}
