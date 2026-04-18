using ImPark.Core.Pathfinding.Domain;

namespace ImPark.Core.Pathfinding.Infrastructure;

// Min-heap keyed by (F, H, TieBreak). Array-backed; grows in powers of two.
// Push/Pop are O(log n); Clear is O(1) (just resets _count, does NOT null
// out slots — PathNode instances are owned by PathNodePool).
public sealed class BinaryHeapOpenSet
{
    private const int InitialCapacity = 64;

    private PathNode[] _heap;
    private int _count;

    public BinaryHeapOpenSet()
    {
        _heap = new PathNode[InitialCapacity];
        _count = 0;
    }

    public int Count => _count;

    public void Clear() => _count = 0;

    public void Push(PathNode node)
    {
        if (_count == _heap.Length)
        {
            var bigger = new PathNode[_heap.Length * 2];
            Array.Copy(_heap, bigger, _heap.Length);
            _heap = bigger;
        }

        _heap[_count] = node;
        SiftUp(_count);
        _count++;
    }

    public PathNode Pop()
    {
        if (_count == 0)
            throw new InvalidOperationException("Heap is empty.");

        var top = _heap[0];
        _count--;

        if (_count > 0)
        {
            _heap[0] = _heap[_count];
            SiftDown(0);
        }

        _heap[_count] = null!;
        return top;
    }

    public PathNode Peek()
    {
        if (_count == 0)
            throw new InvalidOperationException("Heap is empty.");
        return _heap[0];
    }

    private void SiftUp(int index)
    {
        var node = _heap[index];
        while (index > 0)
        {
            int parent = (index - 1) >> 1;
            var parentNode = _heap[parent];
            if (Compare(node, parentNode) >= 0) break;

            _heap[index] = parentNode;
            index = parent;
        }
        _heap[index] = node;
    }

    private void SiftDown(int index)
    {
        var node = _heap[index];
        int half = _count >> 1;

        while (index < half)
        {
            int left = (index << 1) + 1;
            int right = left + 1;
            int best = left;

            if (right < _count && Compare(_heap[right], _heap[left]) < 0)
                best = right;

            if (Compare(_heap[best], node) >= 0) break;

            _heap[index] = _heap[best];
            index = best;
        }
        _heap[index] = node;
    }

    // Deterministic lexical order: (F, H, TieBreak). All three are resolved
    // before falling back to insertion order.
    private static int Compare(PathNode a, PathNode b)
    {
        int c = a.F.CompareTo(b.F);
        if (c != 0) return c;
        c = a.H.CompareTo(b.H);
        if (c != 0) return c;
        return a.TieBreak.CompareTo(b.TieBreak);
    }
}
