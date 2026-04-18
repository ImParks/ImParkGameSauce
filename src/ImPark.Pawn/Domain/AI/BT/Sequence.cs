using BB = ImPark.Pawn.Domain.AI.Blackboard.Blackboard;

namespace ImPark.Pawn.Domain.AI.BT;

public sealed class Sequence : IBTNode
{
    private readonly IBTNode[] children;

    public Sequence(params IBTNode[] children)
    {
        this.children = children ?? Array.Empty<IBTNode>();
    }

    public BTStatus Tick(BB bb, float dt)
    {
        for (int i = 0; i < children.Length; i++)
        {
            var status = children[i].Tick(bb, dt);
            if (status == BTStatus.Failure) return BTStatus.Failure;
            if (status == BTStatus.Running) return BTStatus.Running;
        }
        return BTStatus.Success;
    }
}
