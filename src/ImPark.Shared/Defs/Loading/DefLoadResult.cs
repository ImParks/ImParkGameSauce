namespace ImPark.Shared.Defs.Loading;

public sealed class DefLoadResult
{
    public List<Def> Defs { get; init; } = new();
    public List<DefLoadError> Errors { get; init; } = new();
    public List<DefLoadWarning> Warnings { get; init; } = new();

    public bool HasErrors => Errors.Count > 0;
}

public sealed record DefLoadError(string File, string DefId, string Message);

public sealed record DefLoadWarning(string File, string DefId, string Message);
