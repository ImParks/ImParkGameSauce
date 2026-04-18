using ImPark.Shared.Defs;

namespace ImPark.Pawn.Domain.Defs;

[DefType("PawnThingDef")]
public class PawnThingDef : Def
{
    public RaceProps RaceProps { get; init; } = new();
}
