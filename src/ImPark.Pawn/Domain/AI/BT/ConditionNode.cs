using BB = ImPark.Pawn.Domain.AI.Blackboard.Blackboard;

namespace ImPark.Pawn.Domain.AI.BT;

public sealed class ConditionNode : IBTNode
{
    private readonly Func<BB, bool> predicate;

    public ConditionNode(Func<BB, bool> predicate)
    {
        this.predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    public BTStatus Tick(BB bb, float dt)
        => predicate(bb) ? BTStatus.Success : BTStatus.Failure;
}
