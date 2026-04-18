using ImPark.Core.Pathfinding.Domain;
using ImPark.Shared.Events;
using ImPark.Shared.Geometry;
using ImPark.Shared.Queries;

namespace ImPark.Core.Tests.Pathfinding;

// Simple grid tilemap for A* testing. Walkability and region ids are
// user-supplied; region ids default to 1 for walkable, 0 for blocked.
internal sealed class StubTilemap : ITilemapQuery
{
    private readonly bool[,] _walkable;
    private readonly int[,] _regions;
    private readonly int _w, _h;

    public StubTilemap(int w, int h, bool defaultWalkable = true)
    {
        _w = w; _h = h;
        _walkable = new bool[w, h];
        _regions = new int[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                _walkable[x, y] = defaultWalkable;
                _regions[x, y] = defaultWalkable ? 1 : 0;
            }
    }

    public void SetWall(int x, int y)
    {
        _walkable[x, y] = false;
        _regions[x, y] = 0;
    }

    public void SetRegion(int x, int y, int region) => _regions[x, y] = region;

    public TileData GetTile(int x, int y)
    {
        if (!InBounds(x, y)) return new TileData("oob", false, 0, 0f);
        return new TileData("plain", _walkable[x, y], _regions[x, y], 1.0f);
    }

    public bool IsWalkable(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        return _walkable[x, y];
    }

    public int GetRegionId(int x, int y)
    {
        if (!InBounds(x, y)) return 0;
        return _regions[x, y];
    }

    private bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < _w && y < _h;
}

internal sealed class StubEventBus : IEventBus
{
    public readonly Dictionary<Type, List<Delegate>> Handlers = new();
    public readonly List<(Type Type, object Event)> AsyncQueue = new();
    public readonly List<object> SyncEvents = new();

    public void PublishSync<T>(in T evt) where T : struct
    {
        SyncEvents.Add(evt!);
        if (Handlers.TryGetValue(typeof(T), out var list))
            foreach (var h in list) ((Action<T>)h)(evt);
    }

    public void PublishAsync<T>(in T evt) where T : struct =>
        AsyncQueue.Add((typeof(T), evt));

    public IDisposable Subscribe<T>(Action<T> handler) where T : struct
    {
        if (!Handlers.TryGetValue(typeof(T), out var list))
            Handlers[typeof(T)] = list = new List<Delegate>();
        list.Add(handler);
        return new Unsub(() => list.Remove(handler));
    }

    public void Flush()
    {
        var snap = AsyncQueue.ToList();
        AsyncQueue.Clear();
        foreach (var (t, e) in snap)
            if (Handlers.TryGetValue(t, out var list))
                foreach (var h in list) h.DynamicInvoke(e);
    }

    private sealed class Unsub(Action a) : IDisposable
    {
        public void Dispose() => a();
    }
}

internal sealed class NullTraverseParms : ITraverseParms
{
    public float GetCostModifier(Point p) => 1.0f;
}
