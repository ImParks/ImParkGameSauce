// Phase 2 scope: core fields only (Stages[] + SeverityPerDay defined).
// Phase 4D medical module: stage progression logic lives there.
// When medical module OFF (Phase 2 default), HediffDef.Stages and
// SeverityPerDay are STORED but NOT PROGRESSED per improvement #4.
// BodyPartSystem treats all damage as immediate HP reduction without
// stage advancement.
using System;
using ImPark.Shared.Defs;

namespace ImPark.Pawn.Domain.Defs;

[DefType("HediffDef")]
public class HediffDef : Def
{
    public HediffStage[] Stages { get; init; } = Array.Empty<HediffStage>();
    public float SeverityPerDay { get; init; } = 0f;
}

public class HediffStage
{
    public float MinSeverity { get; init; }
    public string Label { get; init; } = string.Empty;
    public float PainOffset { get; init; }
}
