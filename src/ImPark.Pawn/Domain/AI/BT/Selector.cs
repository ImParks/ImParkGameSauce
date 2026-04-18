using BB = ImPark.Pawn.Domain.AI.Blackboard.Blackboard;

namespace ImPark.Pawn.Domain.AI.BT;

public sealed class Selector : IBTNode
{
    private readonly IBTNode[] children;

    public Selector(params IBTNode[] children)
    {
        this.children = children ?? Array.Empty<IBTNode>();
    }

    public BTStatus Tick(BB bb, float dt)
    {
        for (int i = 0; i < children.Length; i++)
        {
            var status = children[i].Tick(bb, dt);
            if (status == BTStatus.Success) return BTStatus.Success;
            if (status == BTStatus.Running) return BTStatus.Running;
        }
        return BTStatus.Failure;
    }
}
