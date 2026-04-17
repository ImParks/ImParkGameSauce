namespace ImPark.Shared.Defs.Modding;

public sealed class ModManifest
{
    public string Id { get; init; } = string.Empty;
    public string Version { get; init; } = "1.0.0";
    public string Name { get; init; } = string.Empty;
    public List<string> Dependencies { get; init; } = new();
    public int LoadOrder { get; init; }
    public string CompatibleGameVersion { get; init; } = string.Empty;
}
