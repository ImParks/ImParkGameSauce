namespace ImPark.Pawn.Domain.Defs;

public sealed class RaceProps
{
    public string BodyDefId { get; init; } = string.Empty;
    public float BaseHealthScale { get; init; } = 1f;
    public float BaseMoveSpeed { get; init; } = 4f;
}
