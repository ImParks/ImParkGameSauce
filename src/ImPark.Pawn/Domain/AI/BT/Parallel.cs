using BB = ImPark.Pawn.Domain.AI.Blackboard.Blackboard;

namespace ImPark.Pawn.Domain.AI.BT;

public enum ParallelPolicy
{
    RequireAll,
    RequireOne
}

public sealed class Parallel : IBTNode
{
    private readonly IBTNode[] children;
    private readonly ParallelPolicy policy;

    public Parallel(ParallelPolicy policy, params IBTNode[] children)
    {
        this.policy = policy;
        this.children = children ?? Array.Empty<IBTNode>();
    }

    public BTStatus Tick(BB bb, float dt)
    {
        int successCount = 0;
        int failureCount = 0;

        for (int i = 0; i < children.Length; i++)
        {
            var status = children[i].Tick(bb, dt);
            if (status == BTStatus.Success) successCount++;
            else if (status == BTStatus.Failure) failureCount++;
        }

        if (policy == ParallelPolicy.RequireAll)
        {
            if (failureCount > 0) return BTStatus.Failure;
            if (successCount == children.Length) return BTStatus.Success;
            return BTStatus.Running;
        }

        if (successCount > 0) return BTStatus.Success;
        if (failureCount == children.Length) return BTStatus.Failure;
        return BTStatus.Running;
    }
}
