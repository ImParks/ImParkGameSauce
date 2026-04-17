namespace ImPark.Shared.Defs;

public sealed class NullDefDatabase : IDefDatabase
{
    public bool IsSealed => true;

    public T? Get<T>(string defId) where T : Def => null;

    public IReadOnlyList<T> AllOf<T>() where T : Def => Array.Empty<T>();
}
