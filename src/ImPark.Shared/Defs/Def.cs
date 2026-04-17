namespace ImPark.Shared.Defs;

public abstract class Def
{
    public string DefId { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}
