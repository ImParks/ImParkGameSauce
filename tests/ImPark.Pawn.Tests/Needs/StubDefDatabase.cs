using System;
using System.Collections.Generic;
using ImPark.Shared.Defs;

namespace ImPark.Pawn.Tests.Needs;

// Minimal IDefDatabase used across the Needs test suite. Mirrors the stub
// used in Systems/PawnSpawnSystemTests.cs so tests stay self-contained.
internal sealed class StubDefDatabase : IDefDatabase
{
    private readonly Dictionary<Type, Dictionary<string, Def>> buckets = new();

    public bool IsSealed => false;

    public void Add(Def def)
    {
        var type = def.GetType();
        if (!buckets.TryGetValue(type, out var bucket))
        {
            bucket = new Dictionary<string, Def>();
            buckets[type] = bucket;
        }
        bucket[def.DefId] = def;
    }

    public T? Get<T>(string defId) where T : Def
    {
        if (buckets.TryGetValue(typeof(T), out var bucket) &&
            bucket.TryGetValue(defId, out var def))
        {
            return (T)def;
        }
        return null;
    }

    public IReadOnlyList<T> AllOf<T>() where T : Def
    {
        if (!buckets.TryGetValue(typeof(T), out var bucket))
            return Array.Empty<T>();

        var list = new List<T>(bucket.Count);
        foreach (var def in bucket.Values)
            list.Add((T)def);
        return list;
    }
}
