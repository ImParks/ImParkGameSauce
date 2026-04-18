using BB = ImPark.Pawn.Domain.AI.Blackboard.Blackboard;

namespace ImPark.Pawn.Domain.AI.BT;

public sealed class ActionNode : IBTNode
{
    private readonly Func<BB, float, BTStatus> action;

    public ActionNode(Func<BB, float, BTStatus> action)
    {
        this.action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public BTStatus Tick(BB bb, float dt) => action(bb, dt);
}
