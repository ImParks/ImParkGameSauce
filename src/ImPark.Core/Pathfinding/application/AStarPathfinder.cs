using System.Diagnostics;
using ImPark.Core.Pathfinding.Domain;
using ImPark.Core.Pathfinding.Infrastructure;
using ImPark.Shared.Geometry;
using ImPark.Shared.Queries;
using ImPark.Shared.Random;

namespace ImPark.Core.Pathfinding.Application;

public sealed class AStarPathfinder
{
    // Octile constants: unit step = 1.0, diagonal step = sqrt(2).
    // The "+ (sqrt(2)-1) * min(dx,dy)" form is the closed-form heuristic.
    private const float DiagonalCost = 1.41421356f;
    private const float StraightCost = 1.0f;
    private const float SqrtTwoMinusOne = 0.41421356f;

    // Budget check cadence: checking the stopwatch every expansion has
    // measurable overhead, so we check every Nth pop.
    private const int BudgetCheckInterval = 256;

    private readonly ITilemapQuery _tilemap;
    private readonly IRandomService _rng;
    private readonly PathNodePool _pool;
    private readonly BinaryHeapOpenSet _open;
    private readonly Dictionary<long, PathNode> _closed;
    private readonly Dictionary<long, PathNode> _openLookup;
    private readonly HashSet<long> _multiGoalSet;
    private Point[] _goalsForHeuristic = Array.Empty<Point>();

    // 8-directional neighbor offsets. Index pairs (i, i+4) are opposites
    // but that's unused here; we iterate all 8 per expansion.
    private static readonly int[] DX = { 1, -1, 0, 0, 1, 1, -1, -1 };
    private static readonly int[] DY = { 0, 0, 1, -1, 1, -1, 1, -1 };

    public AStarPathfinder(ITilemapQuery tilemap, IRandomService rng)
    {
        _tilemap = tilemap;
        _rng = rng;
        _pool = new PathNodePool();
        _open = new BinaryHeapOpenSet();
        _closed = new Dictionary<long, PathNode>();
        _openLookup = new Dictionary<long, PathNode>();
        _multiGoalSet = new HashSet<long>();
    }

    public PathResult FindPath(
        Point start,
        Point goal,
        float budgetMs = 0f,
        ITraverseParms? parms = null)
    {
        return FindPathCore(start, new[] { goal }, budgetMs, parms);
    }

    public PathResult FindPathToNearestOf(
        Point start,
        Point[] goals,
        float budgetMs = 0f,
        ITraverseParms? parms = null)
    {
        if (goals == null || goals.Length == 0)
            return Fail(PathFailReason.NoPath);

        return FindPathCore(start, goals, budgetMs, parms);
    }

    private PathResult FindPathCore(
        Point start,
        Point[] goals,
        float budgetMs,
        ITraverseParms? parms)
    {
        ResetState();
        _goalsForHeuristic = goals;

        for (int i = 0; i < goals.Length; i++)
            _multiGoalSet.Add(Key(goals[i]));

        if (_multiGoalSet.Contains(Key(start)))
        {
            _pool.ReleaseAll();
            return new PathResult(true, new[] { start }, 0f, false, null);
        }

        if (!_tilemap.IsWalkable(start.X, start.Y))
        {
            _pool.ReleaseAll();
            return Fail(PathFailReason.NoPath);
        }

        var sw = budgetMs > 0f ? Stopwatch.StartNew() : null;
        long budgetTicks = budgetMs > 0f
            ? (long)(budgetMs * Stopwatch.Frequency / 1000.0)
            : long.MaxValue;

        var startNode = _pool.Rent();
        startNode.Pos = start;
        startNode.G = 0f;
        startNode.H = MinOctileToAny(start);
        startNode.F = startNode.H;
        startNode.TieBreak = Hash(start);
        _open.Push(startNode);
        _openLookup[Key(start)] = startNode;

        int expansions = 0;
        PathNode? bestSoFar = startNode;
        float bestSoFarH = startNode.H;

        while (_open.Count > 0)
        {
            var current = _open.Pop();
            long cKey = Key(current.Pos);
            _openLookup.Remove(cKey);

            if (_multiGoalSet.Contains(cKey))
            {
                var wp = Reconstruct(current);
                float cost = current.G;
                _pool.ReleaseAll();
                return new PathResult(true, wp, cost, false, null);
            }

            _closed[cKey] = current;

            if (current.H < bestSoFarH)
            {
                bestSoFar = current;
                bestSoFarH = current.H;
            }

            expansions++;
            if (sw != null && expansions % BudgetCheckInterval == 0 && sw.ElapsedTicks >= budgetTicks)
            {
                var partial = bestSoFar != null ? Reconstruct(bestSoFar) : new[] { start };
                float partialCost = bestSoFar?.G ?? 0f;
                _pool.ReleaseAll();
                return new PathResult(false, partial, partialCost, true, PathFailReason.BudgetExceeded);
            }

            ExpandNeighbors(current, parms);
        }

        _pool.ReleaseAll();
        return Fail(PathFailReason.NoPath);
    }

    private void ExpandNeighbors(PathNode current, ITraverseParms? parms)
    {
        for (int i = 0; i < 8; i++)
        {
            int nx = current.Pos.X + DX[i];
            int ny = current.Pos.Y + DY[i];

            if (!_tilemap.IsWalkable(nx, ny)) continue;

            // Prevent diagonal corner-cutting through walls: for a diagonal
            // move, BOTH cardinal neighbors must also be walkable.
            bool diagonal = DX[i] != 0 && DY[i] != 0;
            if (diagonal)
            {
                if (!_tilemap.IsWalkable(current.Pos.X + DX[i], current.Pos.Y) ||
                    !_tilemap.IsWalkable(current.Pos.X, current.Pos.Y + DY[i]))
                    continue;
            }

            var npos = new Point(nx, ny);
            long nKey = Key(npos);
            if (_closed.ContainsKey(nKey)) continue;

            float stepCost = diagonal ? DiagonalCost : StraightCost;
            if (parms != null)
            {
                float mod = parms.GetCostModifier(npos);
                if (float.IsPositiveInfinity(mod)) continue;
                stepCost *= mod;
            }

            float tentativeG = current.G + stepCost;

            if (_openLookup.TryGetValue(nKey, out var existing))
            {
                if (tentativeG >= existing.G) continue;
                existing.Parent = current;
                existing.G = tentativeG;
                existing.F = tentativeG + existing.H;
            }
            else
            {
                var neighbor = _pool.Rent();
                neighbor.Pos = npos;
                neighbor.Parent = current;
                neighbor.G = tentativeG;
                neighbor.H = MinOctileToAny(npos);
                neighbor.F = neighbor.G + neighbor.H;
                neighbor.TieBreak = Hash(npos);
                _open.Push(neighbor);
                _openLookup[nKey] = neighbor;
            }
        }
    }

    private static Point[] Reconstruct(PathNode end)
    {
        int count = 0;
        var cursor = end;
        while (cursor != null) { count++; cursor = cursor.Parent; }

        var result = new Point[count];
        cursor = end;
        for (int i = count - 1; i >= 0; i--)
        {
            result[i] = cursor!.Pos;
            cursor = cursor.Parent;
        }
        return result;
    }

    public static float Octile(Point a, Point b)
    {
        int dx = Math.Abs(a.X - b.X);
        int dy = Math.Abs(a.Y - b.Y);
        int max = dx > dy ? dx : dy;
        int min = dx < dy ? dx : dy;
        return max + SqrtTwoMinusOne * min;
    }

    // Multi-goal admissible heuristic: min octile distance to any goal.
    // For single-goal calls this collapses to plain octile.
    private float MinOctileToAny(Point p)
    {
        float best = float.PositiveInfinity;
        for (int i = 0; i < _goalsForHeuristic.Length; i++)
        {
            float h = Octile(p, _goalsForHeuristic[i]);
            if (h < best) best = h;
        }
        return best;
    }

    private void ResetState()
    {
        _open.Clear();
        _closed.Clear();
        _openLookup.Clear();
        _multiGoalSet.Clear();
    }

    private static long Key(Point p) => ((long)p.X << 32) ^ (uint)p.Y;

    // Seeded xorshift scramble of (x,y) for tiebreak — deterministic given
    // the same rng seed. Mixing in the rng state makes two independent runs
    // with different seeds explore in different orders (useful for tests).
    private uint Hash(Point p)
    {
        uint h = (uint)(p.X * 73856093) ^ (uint)(p.Y * 19349663);
        h ^= _rng.GetSeed();
        h ^= h << 13;
        h ^= h >> 17;
        h ^= h << 5;
        return h;
    }

    private static PathResult Fail(PathFailReason reason) =>
        new(false, Array.Empty<Point>(), 0f, false, reason);
}
