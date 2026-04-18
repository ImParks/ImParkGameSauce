using ImPark.Pawn.Domain.AI.BT;
using ImPark.Shared.ECS;
using BB = ImPark.Pawn.Domain.AI.Blackboard.Blackboard;

namespace ImPark.Pawn.Domain.Components;

// RULE-001 deviation: holds class references (Blackboard, IBTNode Root).
// Root is transient and rebuilt from ThinkTreeDef at load time via IBTLeafRegistry.
// ThinkTreeDefId is the serialized anchor.
public struct BehaviorTreeComponent : IComponent
{
    public string ThinkTreeDefId;
    public BB BB;
    public IBTNode? Root;
    public long CurrentNodeIndex;
}
