using ImPark.Core.Pathfinding.Domain;

namespace ImPark.Core.Pathfinding.Infrastructure;

// Object pool for PathNode. Steady-state A* iterations must not allocate —
// nodes are rented into an internal list, then all returned in one call via
// ReleaseAll() when the search completes.
public sealed class PathNodePool
{
    private readonly Stack<PathNode> _free = new();
    private readonly List<PathNode> _rented = new();

    public PathNode Rent()
    {
        PathNode node;
        if (_free.Count > 0)
        {
            node = _free.Pop();
            node.Reset();
        }
        else
        {
            node = new PathNode();
        }

        _rented.Add(node);
        return node;
    }

    public void ReleaseAll()
    {
        for (int i = 0; i < _rented.Count; i++)
        {
            var node = _rented[i];
            node.Reset();
            _free.Push(node);
        }

        _rented.Clear();
    }

    public int FreeCount => _free.Count;
    public int RentedCount => _rented.Count;
}
