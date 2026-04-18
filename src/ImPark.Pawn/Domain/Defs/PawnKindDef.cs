using System;
using ImPark.Shared.Defs;

namespace ImPark.Pawn.Domain.Defs;

[DefType("PawnKindDef")]
public class PawnKindDef : Def
{
    public string DefaultName { get; init; } = string.Empty;
    public string ThingDefId { get; init; } = string.Empty;
    public string DefaultThinkTreeDefId { get; init; } = string.Empty;
    public string[] StartingGearDefIds { get; init; } = Array.Empty<string>();
}
