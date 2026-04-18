using ImPark.Core.Pathfinding.Application;
using ImPark.Core.Pathfinding.Domain;
using ImPark.Pawn.Domain.AI.BT;
using ImPark.Shared.Geometry;
using BB = ImPark.Pawn.Domain.AI.Blackboard.Blackboard;

namespace ImPark.Pawn.Application.AI.Leaves;

// Leaf that requests a path from the pawn's current position (bb["self_pos"]) to
// bb[targetKey] and waits for the async result stored at bb[handleKey + "_result"].
// A separate callback wired by the caller is expected to set the result bool.
public sealed class PathActionLeaf : IBTNode
{
    private const string SelfPosKey = "self_pos";
    private const string ResultSuffix = "_result";
    private const int DefaultPriority = 0;

    private readonly IPathRequestService pathSvc;
    private readonly string targetKey;
    private readonly string handleKey;

    public PathActionLeaf(IPathRequestService pathSvc, string targetKey, string handleKey)
    {
        this.pathSvc = pathSvc ?? throw new ArgumentNullException(nameof(pathSvc));
        this.targetKey = targetKey;
        this.handleKey = handleKey;
    }

    public BTStatus Tick(BB bb, float dt)
    {
        if (!bb.Has(handleKey))
        {
            if (!bb.TryGet<Point>(targetKey, out var goal))
                return BTStatus.Failure;
            if (!bb.TryGet<Point>(SelfPosKey, out var start))
                return BTStatus.Failure;

            var resultKey = handleKey + ResultSuffix;
            var handle = pathSvc.Request(
                entityId: (int)bb.PawnId,
                start: start,
                goal: goal,
                priority: DefaultPriority,
                cb: r => bb.Set(resultKey, r.Success));
            bb.Set(handleKey, handle.Id);
            return BTStatus.Running;
        }

        if (bb.TryGet<bool>(handleKey + ResultSuffix, out var ok))
        {
            bb.Remove(handleKey);
            bb.Remove(handleKey + ResultSuffix);
            return ok ? BTStatus.Success : BTStatus.Failure;
        }

        return BTStatus.Running;
    }
}
