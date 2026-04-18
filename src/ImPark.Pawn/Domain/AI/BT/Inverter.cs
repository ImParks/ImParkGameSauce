using BB = ImPark.Pawn.Domain.AI.Blackboard.Blackboard;

namespace ImPark.Pawn.Domain.AI.BT;

public sealed class Inverter : IBTNode
{
    private readonly IBTNode child;

    public Inverter(IBTNode child)
    {
        this.child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public BTStatus Tick(BB bb, float dt)
    {
        var status = child.Tick(bb, dt);
        return status switch
        {
            BTStatus.Success => BTStatus.Failure,
            BTStatus.Failure => BTStatus.Success,
            _ => BTStatus.Running,
        };
    }
}
