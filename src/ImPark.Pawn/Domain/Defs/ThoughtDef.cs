using ImPark.Shared.Defs;

namespace ImPark.Pawn.Domain.Defs;

// Balance data for a thought (mood modifier applied for DurationTicks).
// DurationTicks default = 60 * 300 = 18000 ticks ~= 5 minutes at 60 TPS.
// Stackable=false: re-adding resets duration instead of duplicating.
[DefType("ThoughtDef")]
public class ThoughtDef : Def
{
    public float Offset { get; init; } = 0f;
    public float DurationTicks { get; init; } = 60 * 300;
    public bool Stackable { get; init; } = false;
}
