using ImPark.Shared.Defs;

namespace ImPark.Pawn.Domain.Defs;

// Balance data for a single need (e.g. Hunger, Sleep).
// ThreshPercentByLevel lists the "critical" thresholds ordered ascending
// (e.g. 0.15 = urgent, 0.35 = low, 0.7 = fine). Crossing any threshold
// downward emits NeedCriticalEvent with the current Level.
[DefType("NeedDef")]
public class NeedDef : Def
{
    public float FallPerDay { get; init; } = 1.0f;
    public float[] ThreshPercentByLevel { get; init; } = new[] { 0.15f, 0.35f, 0.7f };
    public bool Disabled { get; init; } = false;
    public int MinAge { get; init; } = 0;
}
