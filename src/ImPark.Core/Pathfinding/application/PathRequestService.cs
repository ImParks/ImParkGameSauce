using ImPark.Core.Pathfinding.Domain;
using ImPark.Shared.Events;
using ImPark.Shared.Geometry;
using ImPark.Shared.Queries;

namespace ImPark.Core.Pathfinding.Application;

public sealed class PathRequestService : IPathRequestService
{
    // RULE-003: tick budget knobs pulled to named constants.
    public const float DefaultBudgetMs = 4.0f;
    public const int MaxRequestsPerTick = 8;

    private readonly AStarPathfinder _astar;
    private readonly RegionReachabilityChecker _reachability;
    private readonly PathCache _cache;
    private readonly IEventBus _eventBus;
    private readonly ITilemapQuery _tilemap;

    private readonly List<QueuedRequest> _queue = new();
    private readonly HashSet<ulong> _cancelled = new();
    private readonly Dictionary<ulong, QueuedRequest> _inFlight = new();

    private ulong _nextHandleId = 1;
    private int _generation;

    public PathRequestService(
        AStarPathfinder astar,
        RegionReachabilityChecker reachability,
        PathCache cache,
        IEventBus eventBus,
        ITilemapQuery tilemap)
    {
        _astar = astar;
        _reachability = reachability;
        _cache = cache;
        _eventBus = eventBus;
        _tilemap = tilemap;
    }

    public int QueueCount => _queue.Count;
    public IReadOnlyDictionary<ulong, QueuedRequest> InFlight => _inFlight;

    public PathRequestHandle Request(
        int entityId,
        Point start,
        Point goal,
        int priority,
        Action<PathResult> cb,
        ITraverseParms? parms = null)
    {
        var handle = new PathRequestHandle(_nextHandleId++, _generation);
        var req = new QueuedRequest(handle, entityId, start, new[] { goal }, priority, cb, parms, isMultiGoal: false);
        InsertByPriority(req);
        _inFlight[handle.Id] = req;

        _eventBus.PublishAsync(new PathRequestedEvent(handle.Id, entityId, start, goal, priority));
        return handle;
    }

    public PathRequestHandle RequestToNearestOf(
        int entityId,
        Point start,
        Point[] goals,
        int priority,
        Action<PathResult> cb,
        ITraverseParms? parms = null)
    {
        var handle = new PathRequestHandle(_nextHandleId++, _generation);
        var primaryGoal = goals.Length > 0 ? goals[0] : start;
        var req = new QueuedRequest(handle, entityId, start, goals, priority, cb, parms, isMultiGoal: true);
        InsertByPriority(req);
        _inFlight[handle.Id] = req;

        _eventBus.PublishAsync(new PathRequestedEvent(handle.Id, entityId, start, primaryGoal, priority));
        return handle;
    }

    public bool IsReachable(Point from, Point to, ITraverseParms? parms = null)
    {
        return _reachability.IsReachable(from, to);
    }

    public void Cancel(PathRequestHandle h)
    {
        _cancelled.Add(h.Id);
    }

    public void Tick(float budgetMs)
    {
        int processed = 0;
        while (processed < MaxRequestsPerTick && _queue.Count > 0)
        {
            var req = _queue[0];
            _queue.RemoveAt(0);

            if (_cancelled.Contains(req.Handle.Id))
            {
                _cancelled.Remove(req.Handle.Id);
                _inFlight.Remove(req.Handle.Id);
                continue;
            }

            ProcessRequest(req, budgetMs);
            _inFlight.Remove(req.Handle.Id);
            processed++;
        }
    }

    private void ProcessRequest(QueuedRequest req, float budgetMs)
    {
        // Cheap region check first: skip A* entirely for unreachable goals.
        if (!req.IsMultiGoal)
        {
            var goal = req.Goals[0];
            if (!_reachability.IsReachable(req.Start, goal))
            {
                var failed = new PathResult(false, Array.Empty<Point>(), 0f, false, PathFailReason.RegionUnreachable);
                req.Callback(failed);
                _eventBus.PublishSync(new PathFailedEvent(
                    req.Handle.Id, req.EntityId, req.Start, goal, PathFailReason.RegionUnreachable));
                return;
            }

            int parmsHash = req.Parms?.GetHashCode() ?? 0;
            if (_cache.TryGet(req.Start, goal, parmsHash, out var cached) && cached.Success)
            {
                req.Callback(cached);
                EmitComputed(req, cached, goal);
                return;
            }

            var result = _astar.FindPath(req.Start, goal, budgetMs, req.Parms);
            if (result.Success) _cache.Put(req.Start, goal, parmsHash, result);
            req.Callback(result);
            if (result.Success) EmitComputed(req, result, goal);
            else EmitFailed(req, result, goal);
        }
        else
        {
            // Multi-goal: filter to region-reachable goals first.
            var reachable = new List<Point>(req.Goals.Length);
            for (int i = 0; i < req.Goals.Length; i++)
                if (_reachability.IsReachable(req.Start, req.Goals[i]))
                    reachable.Add(req.Goals[i]);

            if (reachable.Count == 0)
            {
                var failed = new PathResult(false, Array.Empty<Point>(), 0f, false, PathFailReason.RegionUnreachable);
                req.Callback(failed);
                var fakeGoal = req.Goals.Length > 0 ? req.Goals[0] : req.Start;
                _eventBus.PublishSync(new PathFailedEvent(
                    req.Handle.Id, req.EntityId, req.Start, fakeGoal, PathFailReason.RegionUnreachable));
                return;
            }

            var result = _astar.FindPathToNearestOf(req.Start, reachable.ToArray(), budgetMs, req.Parms);
            var emitGoal = result.Waypoints.Length > 0 ? result.Waypoints[^1] : reachable[0];
            req.Callback(result);
            if (result.Success) EmitComputed(req, result, emitGoal);
            else EmitFailed(req, result, emitGoal);
        }
    }

    private void EmitComputed(QueuedRequest req, PathResult r, Point goal)
    {
        _eventBus.PublishSync(new PathComputedEvent(
            req.Handle.Id, req.EntityId, req.Start, goal, r.Cost, r.Waypoints.Length, r.Partial));
    }

    private void EmitFailed(QueuedRequest req, PathResult r, Point goal)
    {
        var reason = r.Reason ?? PathFailReason.NoPath;
        _eventBus.PublishSync(new PathFailedEvent(
            req.Handle.Id, req.EntityId, req.Start, goal, reason));
    }

    private void InsertByPriority(QueuedRequest req)
    {
        // Higher priority first. FIFO within same priority.
        int idx = 0;
        while (idx < _queue.Count && _queue[idx].Priority >= req.Priority) idx++;
        _queue.Insert(idx, req);
    }

    public sealed record QueuedRequest(
        PathRequestHandle Handle,
        int EntityId,
        Point Start,
        Point[] Goals,
        int Priority,
        Action<PathResult> Callback,
        ITraverseParms? Parms,
        bool IsMultiGoal);
}
