using ImPark.Pawn.Domain.AI.BT;

namespace ImPark.Pawn.Contracts;

public interface IBTLeafRegistry
{
    void Register(string leafDefId, Func<BTNodeSpec, IBTNode> factory);
    IBTNode Resolve(BTNodeSpec spec);
}
