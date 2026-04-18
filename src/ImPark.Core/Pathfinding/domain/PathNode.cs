using ImPark.Shared.Geometry;

namespace ImPark.Core.Pathfinding.Domain;

// Heap/open-set node. Class (not struct) so BinaryHeapOpenSet can store
// references and avoid copy-on-update of G/H/F during relaxation.
public sealed class PathNode
{
    public Point Pos;
    public float G;
    public float H;
    public float F;
    public PathNode? Parent;
    public uint TieBreak;

    public void Reset()
    {
        Pos = default;
        G = 0f;
        H = 0f;
        F = 0f;
        Parent = null;
        TieBreak = 0u;
    }
}
