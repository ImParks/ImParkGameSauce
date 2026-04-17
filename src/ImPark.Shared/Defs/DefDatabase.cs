namespace ImPark.Shared.Defs;

public sealed class DefDatabase : IDefDatabase
{
    private static readonly Lazy<DefDatabase> LazyInstance = new(() => new DefDatabase());
    public static DefDatabase Instance => LazyInstance.Value;

    private readonly Dictionary<Type, Dictionary<string, Def>> _store = new();
    private bool _sealed;

    private DefDatabase() { }

    public bool IsSealed => _sealed;

    public void Register(Def def)
    {
        ThrowIfSealed();

        if (string.IsNullOrWhiteSpace(def.DefId))
            throw new ArgumentException("Def must have a non-empty DefId.");

        var type = def.GetType();
        if (!_store.TryGetValue(type, out var bucket))
        {
            bucket = new Dictionary<string, Def>();
            _store[type] = bucket;
        }

        bucket[def.DefId] = def;
    }

    public T? Get<T>(string defId) where T : Def
    {
        if (_store.TryGetValue(typeof(T), out var bucket) &&
            bucket.TryGetValue(defId, out var def))
        {
            return (T)def;
        }

        return null;
    }

    public IReadOnlyList<T> AllOf<T>() where T : Def
    {
        if (_store.TryGetValue(typeof(T), out var bucket))
            return bucket.Values.Cast<T>().ToList().AsReadOnly();

        return Array.Empty<T>();
    }

    public void Seal()
    {
        _sealed = true;
    }

    public void Clear()
    {
        _store.Clear();
        _sealed = false;
    }

    private void ThrowIfSealed()
    {
        if (_sealed)
            throw new InvalidOperationException("DefDatabase is sealed. No modifications allowed after loading.");
    }
}
