using System;
using ImPark.Shared.Defs;

namespace ImPark.Pawn.Domain.Defs;

[DefType("BodyDef")]
public class BodyDef : Def
{
    public BodyPartNode Root { get; init; } = new();
}

public class BodyPartNode
{
    public string PartId { get; init; } = string.Empty;
    public float HpRatio { get; init; } = 1f;
    public bool Critical { get; init; }
    public BodyPartNode[] Children { get; init; } = Array.Empty<BodyPartNode>();
}
