using ImPark.Core.Pathfinding.Domain;
using ImPark.Shared.Geometry;

namespace ImPark.Core.Pathfinding.Application;

// LRU cache keyed by (start, goal, parmsHash). Capacity default 128.
// Exposes EvictContaining(tile) for PathInvalidationHandler.
public sealed class PathCache
{
    public const int DefaultCapacity = 128;

    private readonly int _capacity;
    private readonly LinkedList<Entry> _lru = new();
    private readonly Dictionary<CacheKey, LinkedListNode<Entry>> _lookup = new();

    public PathCache(int capacity = DefaultCapacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public int Count => _lookup.Count;

    public bool TryGet(Point start, Point goal, int parmsHash, out PathResult result)
    {
        var key = new CacheKey(start, goal, parmsHash);
        if (_lookup.TryGetValue(key, out var node))
        {
            _lru.Remove(node);
            _lru.AddFirst(node);
            result = node.Value.Result;
            return true;
        }

        result = default;
        return false;
    }

    public void Put(Point start, Point goal, int parmsHash, PathResult result)
    {
        var key = new CacheKey(start, goal, parmsHash);
        if (_lookup.TryGetValue(key, out var existing))
        {
            _lru.Remove(existing);
            _lookup.Remove(key);
        }

        var node = new LinkedListNode<Entry>(new Entry(key, result));
        _lru.AddFirst(node);
        _lookup[key] = node;

        while (_lookup.Count > _capacity)
        {
            var last = _lru.Last!;
            _lru.RemoveLast();
            _lookup.Remove(last.Value.Key);
        }
    }

    // Remove all cached paths whose waypoint list includes the given tile.
    // Returns the count of evicted entries.
    public int EvictContaining(Point tile)
    {
        var toRemove = new List<LinkedListNode<Entry>>();
        var cursor = _lru.First;
        while (cursor != null)
        {
            var wps = cursor.Value.Result.Waypoints;
            if (wps != null)
            {
                for (int i = 0; i < wps.Length; i++)
                {
                    if (wps[i] == tile) { toRemove.Add(cursor); break; }
                }
            }
            cursor = cursor.Next;
        }

        for (int i = 0; i < toRemove.Count; i++)
        {
            _lru.Remove(toRemove[i]);
            _lookup.Remove(toRemove[i].Value.Key);
        }

        return toRemove.Count;
    }

    public void Clear()
    {
        _lru.Clear();
        _lookup.Clear();
    }

    private readonly record struct CacheKey(Point Start, Point Goal, int ParmsHash);

    private readonly record struct Entry(CacheKey Key, PathResult Result);
}
