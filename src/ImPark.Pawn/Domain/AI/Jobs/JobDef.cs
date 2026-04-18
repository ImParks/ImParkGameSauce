using ImPark.Shared.Defs;

namespace ImPark.Pawn.Domain.AI.Jobs;

[DefType("JobDef")]
public class JobDef : Def
{
    public string DriverDefId { get; init; } = string.Empty;
    public bool Interruptible { get; init; } = true;
}
