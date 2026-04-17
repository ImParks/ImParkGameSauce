namespace ImPark.Shared.ECS;

public sealed class World
{
    private long nextEntityId = 1;
    private readonly HashSet<long> aliveEntities = new();
    private readonly Dictionary<Type, object> stores = new();

    // Reusable buffer to avoid per-frame allocation in Query methods
    private readonly List<EntityId> queryBuffer = new();

    public EntityId CreateEntity()
    {
        var id = new EntityId(nextEntityId++);
        aliveEntities.Add(id.Value);
        return id;
    }

    public void DestroyEntity(EntityId entity)
    {
        ThrowIfDead(entity);

        foreach (var store in stores.Values)
        {
            if (store is IComponentStoreInternal internalStore)
                internalStore.TryRemove(entity);
        }

        aliveEntities.Remove(entity.Value);
    }

    public bool IsAlive(EntityId entity) => aliveEntities.Contains(entity.Value);

    public int EntityCount => aliveEntities.Count;

    public void AddComponent<T>(EntityId entity, T component) where T : struct, IComponent
    {
        ThrowIfDead(entity);
        GetOrCreateStore<T>().Add(entity, component);
    }

    public ref T GetComponent<T>(EntityId entity) where T : struct, IComponent
    {
        ThrowIfDead(entity);
        return ref GetStore<T>().Get(entity);
    }

    public bool TryGetComponent<T>(EntityId entity, out T component) where T : struct, IComponent
    {
        if (!aliveEntities.Contains(entity.Value))
        {
            component = default;
            return false;
        }

        if (!stores.TryGetValue(typeof(T), out var raw))
        {
            component = default;
            return false;
        }

        return ((ComponentStore<T>)raw).TryGet(entity, out component);
    }

    public void RemoveComponent<T>(EntityId entity) where T : struct, IComponent
    {
        ThrowIfDead(entity);
        GetStore<T>().Remove(entity);
    }

    public bool HasComponent<T>(EntityId entity) where T : struct, IComponent
    {
        if (!aliveEntities.Contains(entity.Value)) return false;
        if (!stores.TryGetValue(typeof(T), out var raw)) return false;
        return ((ComponentStore<T>)raw).Has(entity);
    }

    public List<EntityId> Query<T1>() where T1 : struct, IComponent
    {
        queryBuffer.Clear();

        if (!stores.TryGetValue(typeof(T1), out var raw))
            return queryBuffer;

        var store = (ComponentStore<T1>)raw;
        var entities = store.GetEntities();

        for (int i = 0; i < entities.Length; i++)
            queryBuffer.Add(entities[i]);

        return queryBuffer;
    }

    public List<EntityId> Query<T1, T2>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        queryBuffer.Clear();

        if (!stores.TryGetValue(typeof(T1), out var raw1) ||
            !stores.TryGetValue(typeof(T2), out var raw2))
            return queryBuffer;

        var store1 = (ComponentStore<T1>)raw1;
        var store2 = (ComponentStore<T2>)raw2;

        if (store1.Count <= store2.Count)
        {
            var entities = store1.GetEntities();
            for (int i = 0; i < entities.Length; i++)
            {
                if (store2.Has(entities[i]))
                    queryBuffer.Add(entities[i]);
            }
        }
        else
        {
            var entities = store2.GetEntities();
            for (int i = 0; i < entities.Length; i++)
            {
                if (store1.Has(entities[i]))
                    queryBuffer.Add(entities[i]);
            }
        }

        return queryBuffer;
    }

    public List<EntityId> Query<T1, T2, T3>()
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
    {
        queryBuffer.Clear();

        if (!stores.TryGetValue(typeof(T1), out var raw1) ||
            !stores.TryGetValue(typeof(T2), out var raw2) ||
            !stores.TryGetValue(typeof(T3), out var raw3))
            return queryBuffer;

        var store1 = (ComponentStore<T1>)raw1;
        var store2 = (ComponentStore<T2>)raw2;
        var store3 = (ComponentStore<T3>)raw3;

        var minCount = Math.Min(store1.Count, Math.Min(store2.Count, store3.Count));

        if (minCount == store1.Count)
        {
            var entities = store1.GetEntities();
            for (int i = 0; i < entities.Length; i++)
                if (store2.Has(entities[i]) && store3.Has(entities[i]))
                    queryBuffer.Add(entities[i]);
        }
        else if (minCount == store2.Count)
        {
            var entities = store2.GetEntities();
            for (int i = 0; i < entities.Length; i++)
                if (store1.Has(entities[i]) && store3.Has(entities[i]))
                    queryBuffer.Add(entities[i]);
        }
        else
        {
            var entities = store3.GetEntities();
            for (int i = 0; i < entities.Length; i++)
                if (store1.Has(entities[i]) && store2.Has(entities[i]))
                    queryBuffer.Add(entities[i]);
        }

        return queryBuffer;
    }

    private ComponentStore<T> GetOrCreateStore<T>() where T : struct, IComponent
    {
        if (stores.TryGetValue(typeof(T), out var raw))
            return (ComponentStore<T>)raw;

        var store = new ComponentStore<T>();
        stores[typeof(T)] = store;
        return store;
    }

    private ComponentStore<T> GetStore<T>() where T : struct, IComponent
    {
        if (!stores.TryGetValue(typeof(T), out var raw))
            throw new InvalidOperationException(
                $"No store registered for component {typeof(T).Name}.");

        return (ComponentStore<T>)raw;
    }

    private void ThrowIfDead(EntityId entity)
    {
        if (!aliveEntities.Contains(entity.Value))
            throw new InvalidOperationException(
                $"Entity {entity} is not alive.");
    }
}
