using ImPark.Pawn.Domain.AI.BT;
using ImPark.Shared.Defs;

namespace ImPark.Pawn.Domain.Defs;

[DefType("ThinkTreeDef")]
public class ThinkTreeDef : Def
{
    public BTNodeSpec? RootNode { get; init; }
}
