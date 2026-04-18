using BB = ImPark.Pawn.Domain.AI.Blackboard.Blackboard;

namespace ImPark.Pawn.Domain.AI.BT;

public interface IBTNode
{
    BTStatus Tick(BB bb, float dt);
}
