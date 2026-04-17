namespace ImPark.Shared.Defs;

public interface IDefDatabase
{
    T? Get<T>(string defId) where T : Def;
    IReadOnlyList<T> AllOf<T>() where T : Def;
    bool IsSealed { get; }
}
